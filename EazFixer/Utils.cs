using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;

namespace EazFixer
{
    public static class Utils
    {
        public static IEnumerable<MethodDef> GetMethodsRecursive(ModuleDef t) => t.Types.SelectMany(GetMethodsRecursive);
        public static IEnumerable<MethodDef> GetMethodsRecursive(TypeDef type)
        {
            //return all methods in this type
            foreach (MethodDef m in type.Methods)
                yield return m;

            //go through nested types
            foreach (TypeDef t in type.NestedTypes)
            foreach (MethodDef m in GetMethodsRecursive(t))
                yield return m;
        }

        public static MethodInfo FindMethod(Assembly ass, MethodDef meth, Type[] args)
        {
            var flags = BindingFlags.Default;
            flags |= meth.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flags |= meth.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

            //BUG: this can fail
            Type type = ass.GetType(meth.DeclaringType.ReflectionFullName);
            return type?.GetMethod(meth.Name, flags, null, args, null);
        }

        public static bool LookForReferences(ModuleDef mod, MethodDef meth) //methoddef can be generalized
        {
            //Why LINQ you may ask? Because I can :)
            return GetMethodsRecursive(mod)
                .Where(m => m.HasBody && m.Body.HasInstructions)
                .SelectMany(m => m.Body.Instructions)
                .Any(i => i.Operand != null && i.Operand == meth);
        }
    }
}
