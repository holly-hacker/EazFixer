using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;

namespace EazFixer.Code.Other {
    internal class Utils {
        public static IEnumerable<MethodDef> GetMethodsRecursive(ModuleDef t) {
            return t.Types.SelectMany(GetMethodsRecursive);
        }

        public static IEnumerable<MethodDef> GetMethodsRecursive(TypeDef type) {
            //return all methods in this type
            foreach (MethodDef m in type.Methods)
                yield return m;

            //go through nested types
            foreach (TypeDef t in type.NestedTypes)
            foreach (var m in GetMethodsRecursive(t))
                yield return m;
        }

        public static MethodInfo FindMethod(Assembly assembly, MethodDef method, Type[] args) {
            var flags = BindingFlags.Default;
            flags |= method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flags |= method.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

            //BUG: this can fail
            var type = assembly.GetType(method.DeclaringType.ReflectionFullName);
            return type?.GetMethod(method.Name, flags, null, args, null);
        }

        public static bool IsReferenced(ModuleDef module, MethodDef method) {
            if (GetMethodsRecursive(module)
                .Where(m => m.HasBody && m.Body.HasInstructions)
                .SelectMany(m => m.Body.Instructions)
                .Any(i => i.Operand != null && i.Operand == method)) return true;

            return false;
        }
    }
}