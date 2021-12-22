using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace EazFixer.Processors
{
    internal class StringFixer : ProcessorBase
    {
        private MethodDefinition _decrypterMethod;

        protected override void InitializeInternal()
        {
            //find method
            _decrypterMethod = StringFixUtils.FindStringDecryptMethod(Ctx.Module) ??
                               throw new Exception("Could not find decrypter method");
        }

        protected override void ProcessInternal()
        {
            //a dictionary to cache all strings
            var dictionary = new Dictionary<int, string>();

            //get the decrypter method in a way in which we can invoke it
            var decrypter = Utils.FindMethod(Ctx.Assembly, _decrypterMethod, new[] { typeof(int) }) ?? throw new Exception("Couldn't find decrypter method through reflection");

            //store it so we can use it in the stacktrace patch
            StacktracePatcher.PatchStackTraceGetMethod.MethodToReplace = decrypter;

            //for every method with a body...
            foreach (MethodDefinition meth in Utils.GetMethodsRecursive(Ctx.Module).Where(a => a.CilMethodBody != null && a.CilMethodBody.Instructions.Any()))
            {
                //.. and every instruction (starting at the second one) ...
                for (int i = 1; i < meth.CilMethodBody.Instructions.Count; i++)
                {
                    //get this instruction and the previous
                    var prev = meth.CilMethodBody.Instructions[i - 1];
                    var curr = meth.CilMethodBody.Instructions[i];

                    //if they invoke the string decrypter method with an int parameter
                    if (prev.IsLdcI4() && curr.Operand != null && curr.Operand is MethodDefinition md && md.MetadataToken == _decrypterMethod.MetadataToken)
                    {
                        //get the int parameter, and get the resulting string from either cache or invoking the decrypter method
                        int val = prev.GetLdcI4Constant();
                        if (!dictionary.ContainsKey(val))
                            dictionary[val] = (string) decrypter.Invoke(null, new object[] {val});
                            
                        // check if str == .ctor due to eaz using string decryptor to call constructors
                        if (dictionary[val] == ".ctor" && Flags.VirtFix) continue;

                        //replace the instructions with the string
                        prev.OpCode = CilOpCodes.Nop;
                        curr.OpCode = CilOpCodes.Ldstr;
                        curr.Operand = dictionary[val];
                    }
                }
            }
        }

        protected override void CleanupInternal()
        {
            //check if virtfix is active so ignore cleaning
            if (Flags.VirtFix)
                throw new Exception("VirtFix enabled, Cannot remove method");

            //ensure that the string decryptor isn't called anywhere
            if (Utils.LookForReferences(Ctx.Module, _decrypterMethod))
                throw new Exception("String decrypter is still being called");

            //remove the string decryptor class
            var stringType = _decrypterMethod.DeclaringType;
            if (!Ctx.Module.TopLevelTypes.Remove(stringType))
                throw new Exception("Could not remove string decrypter class");
        }
    }
}
