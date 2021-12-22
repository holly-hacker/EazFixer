using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace EazFixer
{
    public static class StringFixUtils
    {
        public static MethodDefinition FindStringDecryptMethod(ModuleDefinition module)
        {
            return Utils.GetMethodsRecursive(module).SingleOrDefault(CanBeStringMethod);
        }

        private static bool CanBeStringMethod(MethodDefinition method)
        {
            //internal and static
            if (!method.IsStatic || !method.IsAssembly)
                return false;

            //takes int, returns string
            if (method.Signature.ToString() != "System.String *(System.Int32)")
                return false;

            //actually a proper method, not abstract or from an interface
            if (method.CilMethodBody == null || !method.CilMethodBody.Instructions.Any())
                return false;

            //calls the second resolve method (used if string isn't in cache)
            if (!method.CilMethodBody.Instructions.Any(a => a.OpCode.Code == CilCode.Call && a.Operand is MethodDefinition m
                                                                              && m.Signature.ToString() == "System.String *(System.Int32, System.Boolean)"))
                return false;
                
            //is not private or public
            if (method.IsPrivate || method.IsPublic)
                return false;


            return true;
        }
    }
}