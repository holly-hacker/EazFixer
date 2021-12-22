using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types.Parsing;

namespace EazFixer
{
    public static class Utils
    {
        public static IEnumerable<MethodDefinition> GetMethodsRecursive(ModuleDefinition t) => t.GetAllTypes().SelectMany(GetMethodsRecursive);
        public static IEnumerable<MethodDefinition> GetMethodsRecursive(TypeDefinition type)
        {
            //return all methods in this type
            foreach (MethodDefinition m in type.Methods)
                yield return m;

            //go through nested types
            foreach (TypeDefinition t in type.NestedTypes)
            foreach (MethodDefinition m in GetMethodsRecursive(t))
                yield return m;
        }

        public static MethodInfo FindMethod(Assembly ass, MethodDefinition meth, Type[] args)
        {
            var flags = BindingFlags.Default;
            flags |= meth.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flags |= meth.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

            //BUG: this can fail
            // TODO: verify that this works on generics
            string assemblyQualifiedName = TypeNameBuilder.GetAssemblyQualifiedName(meth.DeclaringType.ToTypeSignature());
            Type type = ass.GetType(assemblyQualifiedName);
            return type?.GetMethod(meth.Name, flags, null, args, null);
        }

        public static bool LookForReferences(ModuleDefinition mod, MethodDefinition meth) //methoddef can be generalized
        {
            //Why LINQ you may ask? Because I can :)
            return GetMethodsRecursive(mod)
                .Where(m => m.CilMethodBody != null && m.CilMethodBody.Instructions.Any())
                .SelectMany(m => m.CilMethodBody.Instructions)
                .Any(i => i.Operand != null && i.Operand == meth);
        }
    }
}
