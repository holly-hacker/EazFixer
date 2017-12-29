using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EazFixer.Processors
{
    internal class StringFixer : IProcessor
    {
        private MethodDef _decrypterMethod;

        public void PreProcess(ModuleDef m)
        {
            //find method
            _decrypterMethod = Utils.GetMethodsRecursive(m).Single(CanBeStringMethod);
        }

        public void Process(ModuleDef m)
        {
            //load it normally and get all strings by invoking
            throw new NotImplementedException();
        }

        public void PostProcess(ModuleDef m)
        {
            //replace everything
            throw new NotImplementedException();
        }

        private static bool CanBeStringMethod(MethodDef method)
        {
            if (!method.IsStatic || !method.IsAssembly)
                return false;

            if (method.MethodSig.ToString() != "System.String (System.Int32)")
                return false;

            if (!method.HasBody || !method.Body.HasInstructions)
                return false;

            if (!method.Body.Instructions.Any(a => a.OpCode.Code == Code.Call && a.Operand is MethodDef m
                                                  && m.MethodSig.ToString() == "System.String (System.Int32,System.Boolean)"))
                return false;

            return true;
        }
    }
}
