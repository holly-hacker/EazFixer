using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EazFixer.Processors
{
    internal class ResourceResolver : ProcessorBase
    {
        private TypeDef _resourceResolver;
        private MethodDef _initMethod;

        protected override void InitializeInternal()
        {
            //find all "Resources" classes, and store them for later use
            _resourceResolver = Mod.Types.SingleOrDefault(CanBeResourceResolver) 
                ?? throw new Exception("Could not find resolver type");
            _initMethod = _resourceResolver.Methods.SingleOrDefault(CanBeInitMethod) 
                ?? throw new Exception("Could not find init method");
        }

        protected override void ProcessInternal()
        {
            //initialize all the resources
            var mi = Utils.FindMethod(Asm, _initMethod, new Type[0]);
            mi.Invoke(null, new object[0]);

            //get the dictionary we just initialized
            FieldInfo dictionaryField = mi.DeclaringType.GetFields(BindingFlags.Static | BindingFlags.NonPublic).Single(a => a.FieldType != typeof(Assembly));
            object dictionaryValue = dictionaryField.GetValue(null);
            if (dictionaryValue.GetType().Name != "Dictionary`2") Debug.Fail("not a dictionary");
            var dictionary = (IDictionary)dictionaryValue;

            //extract the assemblies through reflection
            List<Assembly> assemblies = new List<Assembly>();
            foreach (object obj in dictionary.Values) {
                var methods = obj.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).ToList();
                var assembly = (Assembly) methods.Single(a => !a.IsConstructor && a.ReturnParameter?.ParameterType == typeof(Assembly)).Invoke(obj, new object[0]);
                if (!assemblies.Contains(assembly))
                    assemblies.Add(assembly);
            }

            //extract the resources and add them to the module
            foreach (var assembly in assemblies) {
                foreach (Module module in assembly.Modules) {
                    Debug.WriteLine("[D] Loading module for ResourceResolver...");
                    var md = ModuleDefMD.Load(module);

                    foreach (Resource resource in md.Resources)
                        Mod.Resources.Add(resource);
                }
            }
        }

        protected override void CleanupInternal()
        {
            //remove the call to the method that sets OnResourceResolve
            var modType = Mod.GlobalType ?? throw new Exception("Could not find <Module>");
            var instructions = modType.FindStaticConstructor()?.Body?.Instructions ?? throw new Exception("Missing <Module> .cctor");
            foreach (Instruction instr in instructions) {
                if (instr.OpCode.Code != Code.Call) continue;
                if (!(instr.Operand is MethodDef md)) continue;

                if (md.DeclaringType == _resourceResolver)
                    instr.OpCode = OpCodes.Nop;
            }

            Mod.Types.Remove(_resourceResolver);
        }

        private static bool CanBeResourceResolver(TypeDef t)
        {
            if (t.Fields.Count != 2) return false;
            if (t.NestedTypes.Count != 1) return false;

            foreach (MethodDef m in t.Methods.Where(a => a.HasBody && a.Body.HasInstructions)) {
                //adds ResourceResolver
                bool addsResolver = m.Body.Instructions.Any(i => i.OpCode.Code == Code.Callvirt && i.Operand is MemberRef mr && mr.Name == "add_ResourceResolve");
                if (addsResolver) return true;
            }

            return false;
        }

        private static bool CanBeInitMethod(MethodDef a)
        {
            if (!a.HasBody || !a.Body.HasInstructions) return false;

            if (a.MethodSig.ToString() != "System.Void ()") return false;
            
            //might get outdated soon, watch this
            return a.Body.Instructions.First().OpCode.Code == Code.Volatile;
        }
    }
}
