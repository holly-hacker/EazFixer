using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EazFixer.Processors
{
    internal class StringFixer : ProcessorBase
    {
        private MethodDef _decrypterMethod;

        protected override void InitializeInternal()
        {
            //find method
            _decrypterMethod = Utils.GetMethodsRecursive(Mod).Single(CanBeStringMethod);
        }

        protected override void ProcessInternal()
        {
            //a dictionary to cache all strings
            Dictionary<int, string> dictionary = new Dictionary<int, string>();

            //get the decrypter method in a way in which we can invoke it
            var decrypter = Utils.FindMethod(Asm, _decrypterMethod, new[] { typeof(int) }) ?? throw new Exception("Couldn't find decrypter method again");

            //store it so we can use it in the stacktrace patch
            Harmony.PatchStackTraceGetMethod.MethodToReplace = decrypter;

            //for every method with a body...
            foreach (MethodDef meth in Utils.GetMethodsRecursive(Mod).Where(a => a.HasBody && a.Body.HasInstructions))
            {
                //.. and every instruction (starting at the second one) ...
                for (int i = 1; i < meth.Body.Instructions.Count; i++)
                {
                    //get this instruction and the previous
                    var prev = meth.Body.Instructions[i - 1];
                    var curr = meth.Body.Instructions[i];

                    //if they invoke the string decrypter method with an int parameter
                    if (prev.IsLdcI4() && curr.Operand != null && curr.Operand is MethodDef md && new SigComparer().Equals(md, _decrypterMethod))
                    {
                        //get the int parameter, and get the resulting string from either cache or invoking the decrypter method
                        int val = prev.GetLdcI4Value();
                        if (!dictionary.ContainsKey(val))
                            dictionary[val] = (string) decrypter.Invoke(null, new object[] {val});

                        //replace the instructions with the string
                        prev.OpCode = OpCodes.Nop;
                        curr.OpCode = OpCodes.Ldstr;
                        curr.Operand = dictionary[val];
                    }
                }
            }
        }

        protected override void CleanupInternal()
        {
            //ensure that the string decryptor isn't called anywhere
            if (Utils.LookForReferences(Mod, _decrypterMethod))
                throw new Exception("String decrypter is still being called");

            //remove the string decryptor class
            var stringType = _decrypterMethod.DeclaringType;
            Mod.Types.Remove(stringType);
        }

        private static bool CanBeStringMethod(MethodDef method)
        {
            //internal and static
            if (!method.IsStatic || !method.IsAssembly)
                return false;

            //takes int, returns string
            if (method.MethodSig.ToString() != "System.String (System.Int32)")
                return false;

            //actually a proper method, not abstract or from an interface
            if (!method.HasBody || !method.Body.HasInstructions)
                return false;

            //calls the second resolve method (used if string isn't in cache)
            if (!method.Body.Instructions.Any(a => a.OpCode.Code == Code.Call && a.Operand is MethodDef m
                                                  && m.MethodSig.ToString() == "System.String (System.Int32,System.Boolean)"))
                return false;

            return true;
        }
    }
}
