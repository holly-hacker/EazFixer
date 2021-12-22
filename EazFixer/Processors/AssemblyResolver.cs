using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace EazFixer.Processors
{
    internal class AssemblyResolver : ProcessorBase
    {
        private TypeDefinition _assemblyResolver;
        private MethodDefinition _moveNext;
        private List<EmbeddedAssemblyInfo> _assemblies;
        private MethodInfo _decrypter;
        private MethodInfo _decompressor;

        protected override void InitializeInternal()
        {
            //try to find the embedded assemblies string, which is located in 
            //the iterator in the EnumerateEmbeddedAssemblies function
            _assemblyResolver = Ctx.Module.GetAllTypes().SingleOrDefault(CanBeAssemblyResolver)
                                ?? throw new Exception("Could not find resolver type");
            var extractionApi = _assemblyResolver.NestedTypes.SingleOrDefault(CanBeExtractionApi)
                                ?? throw new Exception("Could not find assembly extraction helper");
            var enumerable = extractionApi.NestedTypes.SingleOrDefault(CanBeEnumerable)
                                ?? throw new Exception("Could not find EnumerateEmbeddedAssemblies iterator");
            _moveNext = GetMoveNextMethod(enumerable)
                                ?? throw new Exception("Could not find EnumerateEmbeddedAssemblies's MoveNext");

            //find the decryption methods
            var dec1 = _assemblyResolver.Methods.SingleOrDefault(CanBeDecryptionMethod1)
                                ?? throw new Exception("Could not find decryption method");
            _decrypter = Utils.FindMethod(Ctx.Assembly, dec1, new[] {typeof(byte[])}) 
                ?? throw new Exception("Couldn't find decrypter through reflection");

            var dec2 = _assemblyResolver.Methods.SingleOrDefault(CanBeDecompressionMethod);       //this one may be null
            if (dec2 != null)
                _decompressor = Utils.FindMethod(Ctx.Assembly, dec2, new[] {typeof(byte[])})
                                 ?? throw new Exception("Couldn't find prefix remover through reflection");
        }

        protected override void ProcessInternal()
        {
            //get path to write to
            var path = Path.GetDirectoryName(Ctx.Assembly.Location);

            //get assemblies
            //this happens here because we need strings to be decrypted
            if (!Ctx.Get<StringFixer>().Processed) throw new Exception("StringFixer is required!");
            var str = _moveNext.CilMethodBody.Instructions.SingleOrDefault(a => a.OpCode.Code == CilCode.Ldstr)?.Operand as string
                      ?? throw new Exception("Could not find assembly list");
            _assemblies = EnumerateEmbeddedAssemblies(str).Where(a => !a.Fullname.StartsWith("#A,A,")).ToList();    //TODO: investigate prefix

            //get the resource resolver and figure out which assemblies we shouldn't extract
            var res = Ctx.Get<ResourceResolver>();
            IEnumerable<EmbeddedAssemblyInfo> asmEnumerator = res.Initialized
                ? _assemblies.Where(a => res.ResourceAssemblies.All(b => !string.Equals(b.GetName().Name, new AssemblyName(a.Fullname).Name, StringComparison.CurrentCultureIgnoreCase)))
                : _assemblies;

            foreach (var assembly in asmEnumerator)
            {
                //get the resource containing the assembly
                string resName = assembly.ResourceName;
                var stream = Ctx.Assembly.GetManifestResourceStream(resName);    //not sure if reflection is the best way
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);

                //if the assembly is encrypted: decrypt it
                if (assembly.Encrypted) {
                    _decrypter.Invoke(null, new object[] {buffer});
                }
                //if the assembly is prefixed: remove it
                if (assembly.Compressed) {
                    if (_decompressor == null) throw new Exception("Assembly is compressed, but couldn't find decompressor method");
                    _decompressor.Invoke(null, new object[] {buffer});
                }

                File.WriteAllBytes(Path.Combine(path, assembly.Filename), buffer);
            }
        }

        protected override void CleanupInternal()
        {
            //remove the call to the method that sets OnAssemblyResolve
            var modType = Ctx.Module.GetModuleType() ?? throw new Exception("Could not find <Module>");
            var instructions = modType.GetStaticConstructor()?.CilMethodBody?.Instructions ?? throw new Exception("Missing <Module> .cctor");
            foreach (CilInstruction instr in instructions)
            {
                if (instr.OpCode.Code != CilCode.Call) continue;
                if (!(instr.Operand is MethodDefinition md)) continue;

                if (md.DeclaringType == _assemblyResolver)
                    instr.OpCode = CilOpCodes.Nop;
            }

            //remove types
            if (_decompressor == null) //if it is present, more stuff is going on that I don't know about (better be safe)
            {
                // TODO: verify that this is a top-level type
                Ctx.Module.TopLevelTypes.Remove(_assemblyResolver);
            }

            //remove resources
            foreach (var assembly in _assemblies)
                Ctx.Module.Resources.Remove(Ctx.Module.Resources.SingleOrDefault(a => a.Name == assembly.ResourceName)
                                            ?? throw new Exception("Resource name not unique (or present)"));
        }

        private bool CanBeAssemblyResolver(TypeDefinition t)
        {
            if (t.Methods.Count(a => a.IsPInvokeImpl) != 1) return false;   //MoveFileEx
            if (t.NestedTypes.Count < 4) return false; //AssemblyCache, AssemblyRepresentation, ExtractionApi, RNG

            foreach (MethodDefinition m in t.Methods.Where(a => a.CilMethodBody != null && a.CilMethodBody.Instructions.Any())) {
                //adds AssemblyResolver
                bool addsResolver = m.CilMethodBody.Instructions.Any(i => i.OpCode == CilOpCodes.Callvirt && i.Operand is MemberReference mr && mr.Name == "add_AssemblyResolve");
                if (addsResolver)
                    return true;
            }

            return false;
        }

        private bool CanBeExtractionApi(TypeDefinition t) => t.NestedTypes.Any() && t.IsAbstract && t.IsSealed && t.NestedTypes.Any(CanBeEnumerable);
        private bool CanBeEnumerable(TypeDefinition t) => t.Interfaces.Any() && t.Interfaces.Any(a => a.Interface.Name == "IEnumerable");
        private MethodDefinition GetMoveNextMethod(TypeDefinition t) => t.MethodImplementations
                                                                            .Where(mi =>
                                                                                mi.Declaration.FullName ==
                                                                                "System.Boolean System.Collections.IEnumerator::MoveNext()")
                                                                            .Select(mi => mi.Body)
                                                                            .FirstOrDefault()?
                                                                            .Resolve()
                                                                        ?? throw new Exception(
                                                                            "Could not find MoveNext method");
        private bool CanBeDecryptionMethod1(MethodDefinition m) => m.Signature.ToString() == "System.Byte[] *(System.Byte[])" && m.NoInlining;
        private bool CanBeDecompressionMethod(MethodDefinition m) => m.Signature.ToString() == "System.Byte[] *(System.Byte[])" && !m.NoInlining;

        private static IEnumerable<EmbeddedAssemblyInfo> EnumerateEmbeddedAssemblies(string text)
        {
            var split = text.Split(',');

            for (int i = 0; i < split.Length; i += 4)
            {
                string b64 = split[i];
                string resName = split[i + 1];
                var asm = new EmbeddedAssemblyInfo { FullnameBase64 = b64 };

                //check flags
                int posPipe = resName.IndexOf('|');
                if (posPipe >= 0)
                {
                    string flags = resName.Substring(0, posPipe);
                    resName = resName.Substring(posPipe + 1);

                    asm.Encrypted = flags.IndexOf('a') != -1;
                    asm.Compressed = flags.IndexOf('b') != -1;
                    asm.MustLoadfromDisk = flags.IndexOf('c') != -1;
                }
                asm.ResourceName = resName;
                asm.FilenameBase64 = split[i + 2];

                yield return asm;
            }
        }

        [DebuggerDisplay("{" + nameof(Fullname) + "}")]
        internal class EmbeddedAssemblyInfo
        {
            public string Fullname {
                get {
                    if (_fullname != null) return _fullname;
                    byte[] array = Convert.FromBase64String(FullnameBase64);
                    return _fullname = Encoding.UTF8.GetString(array, 0, array.Length);
                }
            }

            public string Filename {
                get {
                    if (_filename != null) return _filename;
                    byte[] array = Convert.FromBase64String(FilenameBase64);
                    return _filename = Encoding.UTF8.GetString(array, 0, array.Length);
                }
            }

            public string FullnameBase64;
            public string ResourceName;
            public bool Encrypted;
            public bool Compressed;
            public bool MustLoadfromDisk;
            public string FilenameBase64;
            private string _fullname;
            private string _filename;
        }
    }
}
