using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace EazFixer.Processors
{
    internal class ResourceResolver : ProcessorBase
    {
        public List<Assembly> ResourceAssemblies;
        private TypeDefinition _resourceResolver;
        private MethodDefinition _initMethod;

        protected override void InitializeInternal()
        {
            //find all "Resources" classes, and store them for later use
            _resourceResolver = Ctx.Module.GetAllTypes().SingleOrDefault(CanBeResourceResolver)
                ?? throw new Exception("Could not find resolver type");
            _initMethod = _resourceResolver.Methods.SingleOrDefault(CanBeInitMethod) 
                ?? throw new Exception("Could not find init method");
        }

        protected override void ProcessInternal()
        {
            //initialize all the resources
            var mi = Utils.FindMethod(Ctx.Assembly, _initMethod, new Type[0]) ?? throw new Exception("Could not find init method through reflection");
            mi.Invoke(null, new object[0]);

            //get the dictionary we just initialized
            FieldInfo dictionaryField = mi.DeclaringType.GetFields(BindingFlags.Static | BindingFlags.NonPublic).Single(a => a.FieldType != typeof(Assembly));
            object dictionaryValue = dictionaryField.GetValue(null);
            if (dictionaryValue.GetType().Name != "Dictionary`2") Debug.Fail("not a dictionary");
            var dictionary = (IDictionary)dictionaryValue;

            //extract the assemblies through reflection
            ResourceAssemblies = new List<Assembly>();
            foreach (object obj in dictionary.Values) {
                var methods = obj.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).ToList();
                var assembly = (Assembly) methods.Single(a => !a.IsConstructor && a.ReturnParameter?.ParameterType == typeof(Assembly)).Invoke(obj, new object[0]);
                if (!ResourceAssemblies.Contains(assembly))
                    ResourceAssemblies.Add(assembly);
            }

            //extract the resources and add them to the module
            foreach (var assembly in ResourceAssemblies) {
                foreach (Module module in assembly.Modules) {
                    Debug.WriteLine("[D] Loading module for ResourceResolver...");

                    // TODO: verify that this works
                    var baseAddress = Marshal.GetHINSTANCE(module);

                    if (baseAddress == IntPtr.Zero || baseAddress == new IntPtr(-1))
                        throw new Exception("Failed to instantiate resource module");

                    var md = ModuleDefinition.FromModuleBaseAddress(baseAddress);

                    foreach (ManifestResource resource in md.Resources)
                        Ctx.Module.Resources.Add(resource);
                }
            }
        }

        protected override void CleanupInternal()
        {
            //remove the call to the method that sets OnResourceResolve
            var modType = Ctx.Module.GetModuleType() ?? throw new Exception("Could not find <Module>");
            var instructions = modType.GetStaticConstructor()?.CilMethodBody?.Instructions ?? throw new Exception("Missing <Module> .cctor");
            foreach (CilInstruction instr in instructions) {
                if (instr.OpCode != CilOpCodes.Call) continue;
                if (!(instr.Operand is MethodDefinition md)) continue;

                if (md.DeclaringType == _resourceResolver)
                    instr.OpCode = CilOpCodes.Nop;
            }

            if (!Ctx.Module.TopLevelTypes.Remove(_resourceResolver))
                throw new Exception("Could not remove resource resolver type");
        }

        private static bool CanBeResourceResolver(TypeDefinition t)
        {
            if (t.Fields.Count != 2) return false;
            if (t.NestedTypes.Count != 1) return false;

            foreach (MethodDefinition m in t.Methods.Where(a => a.CilMethodBody != null && a.CilMethodBody.Instructions.Any())) {
                //adds ResourceResolver
                bool addsResolver = m.CilMethodBody.Instructions.Any(i => i.OpCode == CilOpCodes.Callvirt && i.Operand is MemberReference mr && mr.Name == "add_ResourceResolve");
                if (addsResolver) return true;
            }

            return false;
        }

        private static bool CanBeInitMethod(MethodDefinition a)
        {
            if (a.CilMethodBody == null || !a.CilMethodBody.Instructions.Any()) return false;

            if (a.Signature.ToString() != "System.Void *()") return false;
            
            //might get outdated soon, watch this
            return a.CilMethodBody.Instructions.First().OpCode.Code == CilCode.Volatile;
        }
    }
}
