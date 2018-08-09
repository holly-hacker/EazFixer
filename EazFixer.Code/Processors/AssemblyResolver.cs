using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EazFixer.Code.Other;

namespace EazFixer.Code.Processors {
    internal class AssemblyResolver : ProcessorBase {
        private List<EmbeddedAssemblyInfo> _assemblies;

        private TypeDef _assemblyResolver;
        private MethodInfo _decrypter;
        private MethodDef _moveNext;
        private MethodInfo _prefixRemover;
        public override string ProcessorName => "AssemblyResolver";

        protected override void InitializeInternal() {
            //try to find the embedded assemblies string, which is located in 
            //the iterator in the EnumerateEmbeddedAssemblies function
            _assemblyResolver = ctx.Module.Types.SingleOrDefault(CanBeAssemblyResolver)
                                ?? throw new Exception("Could not find resolver type");
            var extractionApi = _assemblyResolver.NestedTypes.SingleOrDefault(CanBeExtractionApi)
                                ?? throw new Exception("Could not find assembly extraction helper");
            var enumerable = extractionApi.NestedTypes.SingleOrDefault(CanBeEnumerable)
                             ?? throw new Exception("Could not find EnumerateEmbeddedAssemblies iterator");
            _moveNext = enumerable.Methods.SingleOrDefault(CanBeMoveNext)
                        ?? throw new Exception("Could not find EnumerateEmbeddedAssemblies's MoveNext");

            //find the decryption methods
            var dec1 = _assemblyResolver.Methods.SingleOrDefault(CanBeDecryptionMethod1)
                       ?? throw new Exception("Could not find decryption method");
            _decrypter = Utils.FindMethod(ctx.Assembly, dec1, new[] {typeof(byte[])})
                         ?? throw new Exception("Couldn't find decrypter through reflection");

            var dec2 = _assemblyResolver.Methods.SingleOrDefault(CanBeDecryptionMethod2); //this one may be null
            if (dec2 != null)
                _prefixRemover = Utils.FindMethod(ctx.Assembly, dec2, new[] {typeof(byte[])})
                                 ?? throw new Exception("Couldn't find prefix remover through reflection");
        }

        protected override void ProcessInternal() {
            //get path to write to
            var path = Path.GetDirectoryName(ctx.Assembly.Location);

            //get assemblies
            //this happens here because we need string to be decrypted
            if (!ctx.Get<StringResolver>().Processed) throw new Exception("StringFixer is required!");
            var str = _moveNext.Body.Instructions.SingleOrDefault(a => a.OpCode.Code == dnlib.DotNet.Emit.Code.Ldstr)
                          ?.Operand as string
                      ?? throw new Exception("Could not find assembly list");
            _assemblies =
                EnumerateEmbeddedAssemblies(str).Where(a => !a.Fullname.StartsWith("#A,A,"))
                    .ToList(); //TODO: investigate prefix

            //get the resource resolver and figure out which assemblies we shouldn't extract
            var res = ctx.Get<ResourceResolver>();
            if (res.ResourceAssemblies == null) { //If there is no resources but exists assemblies
                var asmEnumerator = _assemblies;

                EachAssembly(asmEnumerator, path);
            }
            else {
                var asmEnumerator = _assemblies.Where(a => res.ResourceAssemblies.All(b
                    => !string.Equals(b.GetName().Name, new AssemblyName(a.Fullname).Name,
                        StringComparison.CurrentCultureIgnoreCase)));

                EachAssembly(asmEnumerator, path);
            }
        }

        private void EachAssembly(IEnumerable<EmbeddedAssemblyInfo> asmEnumerator, string path) {
            foreach (var assembly in asmEnumerator) {
                //get the resource containing the assembly
                var resName = assembly.ResourceName;
                var stream = ctx.Assembly.GetManifestResourceStream(resName); //not sure if reflection is the best way
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int) stream.Length);

                //if the assembly is encrypted: decrypt it
                if (assembly.Encrypted) _decrypter.Invoke(null, new object[] {buffer});

                //if the assembly is prefixed: remove it
                if (assembly.Prefixed) {
                    if (_prefixRemover == null)
                        throw new Exception("Assembly is prefixed/pumped, but couldn't find prefix remover type");
                    buffer = (byte[]) _prefixRemover.Invoke(null,
                        new object[] {buffer}); //unpack assembly and save in buffer
                }

                File.WriteAllBytes(Path.Combine(path, assembly.Filename), buffer);
            }
        }

        protected override void CleanupInternal() {
            //remove the call to the method that sets OnAssemblyResolve
            var modType = ctx.Module.GlobalType ?? throw new Exception("Could not find <Module>");
            var instructions = modType.FindStaticConstructor()?.Body?.Instructions ??
                               throw new Exception("Missing <Module> .cctor");
            foreach (Instruction instr in instructions) {
                if (instr.OpCode.Code != dnlib.DotNet.Emit.Code.Call) continue;
                if (!(instr.Operand is MethodDef md)) continue;

                if (md.DeclaringType == _assemblyResolver)
                    instr.OpCode = OpCodes.Nop;
            }

            //remove types
            if (_prefixRemover == null
            ) //if it is present, more stuff is going on that I don't know about (better be safe)
                ctx.Module.Types.Remove(_assemblyResolver);

            //remove resources
            foreach (var assembly in _assemblies)
                ctx.Module.Resources.Remove(ctx.Module.Resources.SingleOrDefault(a => a.Name == assembly.ResourceName)
                                            ?? throw new Exception("Resource name not unique (or present)"));
        }

        private bool CanBeAssemblyResolver(TypeDef t) {
            if (t.Methods.Count(a => a.IsPinvokeImpl) != 1) return false; //MoveFileEx
            if (t.NestedTypes.Count < 4) return false; //AssemblyCache, AssemblyRepresentation, ExtractionApi, RNG

            foreach (MethodDef m in t.Methods.Where(a => a.HasBody && a.Body.HasInstructions)) {
                //adds AssemblyResolver
                bool addsResolver = m.Body.Instructions.Any(i =>
                    i.OpCode.Code == dnlib.DotNet.Emit.Code.Callvirt && i.Operand is MemberRef mr &&
                    mr.Name == "add_AssemblyResolve");
                if (addsResolver)
                    return true;
            }

            return false;
        }

        private bool CanBeExtractionApi(TypeDef t) {
            return t.HasNestedTypes && t.IsAbstract && t.IsSealed && t.NestedTypes.Any(CanBeEnumerable);
        }

        private bool CanBeEnumerable(TypeDef t) {
            return t.HasInterfaces && t.Interfaces.Any(a => a.Interface.Name == "IEnumerable");
        }

        private bool CanBeMoveNext(MethodDef m) {
            return m.Overrides.Any(a =>
                a.MethodDeclaration.FullName == "System.Boolean System.Collections.IEnumerator::MoveNext()");
        }

        private bool CanBeDecryptionMethod1(MethodDef m) {
            return m.MethodSig.ToString() == "System.Byte[] (System.Byte[])" && m.IsNoInlining;
        }

        private bool CanBeDecryptionMethod2(MethodDef m) {
            return m.MethodSig.ToString() == "System.Byte[] (System.Byte[])" && !m.IsNoInlining;
        }

        private static IEnumerable<EmbeddedAssemblyInfo> EnumerateEmbeddedAssemblies(string text) {
            var split = text.Split(',');

            for (var i = 0; i < split.Length; i += 4) {
                var b64 = split[i];
                var resName = split[i + 1];
                var asm = new EmbeddedAssemblyInfo {FullnameBase64 = b64};

                //check flags
                var posPipe = resName.IndexOf('|');
                if (posPipe >= 0) {
                    var flags = resName.Substring(0, posPipe);
                    resName = resName.Substring(posPipe + 1);

                    asm.Encrypted = flags.IndexOf('a') != -1;
                    asm.Prefixed = flags.IndexOf('b') != -1;
                    asm.MustLoadfromDisk = flags.IndexOf('c') != -1;
                }

                asm.ResourceName = resName;
                asm.FilenameBase64 = split[i + 2];

                yield return asm;
            }
        }

        internal class EmbeddedAssemblyInfo {
            private string _filename;
            private string _fullname;
            public bool Encrypted;
            public string FilenameBase64;

            public string FullnameBase64;
            public bool MustLoadfromDisk;
            public bool Prefixed;
            public string ResourceName;

            public string Fullname {
                get {
                    if (_fullname != null) return _fullname;
                    var array = Convert.FromBase64String(FullnameBase64);
                    return _fullname = Encoding.UTF8.GetString(array, 0, array.Length);
                }
            }

            public string Filename {
                get {
                    if (_filename != null) return _filename;
                    var array = Convert.FromBase64String(FilenameBase64);
                    return _filename = Encoding.UTF8.GetString(array, 0, array.Length);
                }
            }
        }
    }
}