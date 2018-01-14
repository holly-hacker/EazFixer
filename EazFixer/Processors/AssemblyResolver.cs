using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EazFixer.Processors
{
    internal class AssemblyResolver : ProcessorBase
    {
        private TypeDef _assemblyResolver;
        private List<EmbeddedAssemblyInfo> _assemblies;
        private MethodInfo _decrypter;

        protected override void InitializeInternal()
        {
            //try to find the embedded assemblies string, which is located in 
            //the iterator in the EnumerateEmbeddedAssemblies function
            _assemblyResolver = Ctx.Module.Types.SingleOrDefault(CanBeAssemblyResolver)
                                ?? throw new Exception("Could not find resolver type");
            var extractionApi = _assemblyResolver.NestedTypes.SingleOrDefault(CanBeExtractionApi)
                                ?? throw new Exception("Could not find assembly extraction helper");
            var enumerable = extractionApi.NestedTypes.SingleOrDefault(CanBeEnumerable)
                                ?? throw new Exception("Could not find EnumerateEmbeddedAssemblies iterator");
            var moveNext = enumerable.Methods.SingleOrDefault(CanBeMoveNext)
                                ?? throw new Exception("Could not find EnumerateEmbeddedAssemblies's MoveNext");
            var str = moveNext.Body.Instructions.SingleOrDefault(a => a.OpCode.Code == Code.Ldstr)?.Operand as string
                                ?? throw new Exception("Could not find assembly list");

            _assemblies = EnumerateEmbeddedAssemblies(str).ToList();

            //only if _assemblies.Any(a => a.Encrypted)?
            //find the decryption method
            var decryptionMethodDef = _assemblyResolver.Methods.SingleOrDefault(CanBeDecryptionMethod)
                                ?? throw new Exception("Could not find decryption method");
            _decrypter = Utils.FindMethod(Ctx.Assembly, decryptionMethodDef, new[] {typeof(byte[])});
        }

        protected override void ProcessInternal()
        {
            //get path to write to
            var path = Path.GetDirectoryName(Ctx.Assembly.Location);

            //get the resource resolver and figure out which assemblies we shouldn't extract
            var res = Ctx.Get<ResourceResolver>();
            var asmEnumerator = _assemblies.Where(a => res.ResourceAssemblies.All(b 
                => !string.Equals(b.GetName().Name, new AssemblyName(a.Fullname).Name, StringComparison.CurrentCultureIgnoreCase)));

            foreach (var assembly in asmEnumerator)
            {
                //get the resource containing the assembly
                string resName = assembly.ResourceName;
                var stream = Ctx.Assembly.GetManifestResourceStream(resName);    //not sure if reflection is the best way
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);

                //if the assembly is encrypted: decrypt it
                if (assembly.Encrypted) {
                    Debug.WriteLine("Decrypting assembly...");
                    _decrypter.Invoke(null, new object[] {buffer});
                }

                File.WriteAllBytes(Path.Combine(path, assembly.Filename), buffer);
            }
        }

        protected override void CleanupInternal()
        {
            //TODO: remove resolver type
        }

        private bool CanBeAssemblyResolver(TypeDef t)
        {
            if (t.Methods.Count(a => a.IsPinvokeImpl) != 1) return false;   //MoveFileEx
            if (t.NestedTypes.Count != 4) return false; //AssemblyCache, AssemblyRepresentation, ExtractionApi, RNG

            foreach (MethodDef m in t.Methods.Where(a => a.HasBody && a.Body.HasInstructions)) {
                //adds AssemblyResolver
                bool addsResolver = m.Body.Instructions.Any(i => i.OpCode.Code == Code.Callvirt && i.Operand is MemberRef mr && mr.Name == "add_AssemblyResolve");
                if (addsResolver) return true;
            }

            return false;
        }

        private bool CanBeExtractionApi(TypeDef t) => t.HasNestedTypes;
        private bool CanBeEnumerable(TypeDef t) => t.HasInterfaces;
        private bool CanBeMoveNext(MethodDef m) => m.Overrides.Any(a => a.MethodDeclaration.FullName == "System.Boolean System.Collections.IEnumerator::MoveNext()");

        private bool CanBeDecryptionMethod(MethodDef m) => m.MethodSig.ToString() == "System.Byte[] (System.Byte[])";

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
                    asm.MustLoadfromDisk = flags.IndexOf('c') != -1;
                }
                asm.ResourceName = resName;
                asm.FilenameBase64 = split[i + 2];

                yield return asm;
            }
        }

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
            public bool MustLoadfromDisk;
            public string FilenameBase64;
            private string _fullname;
            private string _filename;
        }
    }
}
