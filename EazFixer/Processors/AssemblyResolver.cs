using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EazFixer.Processors
{
    internal class AssemblyResolver : ProcessorBase
    {
        private TypeDef _assemblyResolver;
        private List<EmbeddedAssembly> _assemblies;

        protected override void InitializeInternal()
        {
            //find the assembly resolver type
            _assemblyResolver = Mod.Types.SingleOrDefault(CanBeAssemblyResolver)
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
        }

        protected override void ProcessInternal()
        {
            //TODO: make sure to not extract the resource dll, do should have done that in ResourceResolver
            throw new NotImplementedException();
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

        private static IEnumerable<EmbeddedAssembly> EnumerateEmbeddedAssemblies(string text)
        {
            var split = text.Split(',');

            for (int i = 0; i < split.Length; i += 4)
            {
                string b64 = split[i];
                string resName = split[i + 1];
                var asm = new EmbeddedAssembly { FullnameBase64 = b64 };

                //possible flags?
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

        internal sealed class EmbeddedAssembly
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
