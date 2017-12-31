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
    internal class ResourceResolver : IProcessor
    {
        private TypeDef _resourceResolver;
        private MethodDef _ensureLoadedMethod;

        private string _file;

        public ResourceResolver(string file)
        {
            _file = file;
        }

        public void PreProcess(ModuleDef m)
        {
            //TODO: don't throw if not present

            //find all "Resources" classes, and store them for later use
            _resourceResolver = m.Types.Single(CanBeResourceResolver);
            _ensureLoadedMethod = _resourceResolver.Methods.Single(CanBeEnsureLoadedMethod);
        }

        public void Process(ModuleDef m)
        {
            List<Assembly> assemblies = new List<Assembly>();

            var mi = Utils.FindMethod(Assembly.LoadFile(_file), _ensureLoadedMethod, new Type[0]);
            mi.Invoke(null, new object[0]);

            var f = mi.DeclaringType.GetFields(BindingFlags.Static | BindingFlags.NonPublic);

            FieldInfo fi = f.Single(a => a.FieldType != typeof(Assembly));
            object value = fi.GetValue(null);
            if (value.GetType().Name != "Dictionary`2") Debug.Fail("not a dictionary");

            var dick = value as IDictionary;

            //each of the entries contains the same resource
            foreach (string s in dick.Keys) {
                var obj = dick[s];
                var t = obj.GetType();
                var methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).ToList();
                var asm = (Assembly) methods.Single(a => !a.IsConstructor && a.ReturnParameter.ParameterType == typeof(Assembly)).Invoke(obj, new object[0]);
                if (!assemblies.Contains(asm))
                    assemblies.Add(asm);
            }

            foreach (var assembly in assemblies) {
                foreach (Module module in assembly.Modules) {
                    Debug.WriteLine("[D] Loading module for ResourceResolver...");
                    var md = ModuleDefMD.Load(module);

                    foreach (Resource resource in md.Resources) {
                        m.Resources.Add(resource);
                    }
                }
            }
        }

        public void PostProcess(ModuleDef m)
        {
            //TODO: remove resource type
        }
        private static bool CanBeResourceResolver(TypeDef t)
        {
            //TODO: preliminary check
            if (t.Fields.Count != 2) return false;
            if (t.NestedTypes.Count != 1) return false;

            foreach (MethodDef m in t.Methods.Where(a => a.HasBody && a.Body.HasInstructions)) {
                //adds ResourceResolver
                bool addsResolver = m.Body.Instructions.Any(i => i.OpCode.Code == Code.Callvirt && i.Operand is MemberRef mr && mr.Name == "add_ResourceResolve");
                if (addsResolver) return true;
            }

            return false;
        }

        private static bool CanBeEnsureLoadedMethod(MethodDef a)
        {
            if (!a.HasBody || !a.Body.HasInstructions) return false;

            if (a.MethodSig.ToString() != "System.Void ()") return false;
            
            //might get outdated soon, watch this
            return a.Body.Instructions.First().OpCode.Code == Code.Volatile;
        }
    }
}
