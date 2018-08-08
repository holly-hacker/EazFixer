using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EazFixer.Code.Other;

namespace EazFixer.Code.Processors {
    internal class StringResolver : ProcessorBase {
        private MethodDef _decrypterMethod;
        public override string ProcessorName => "StringResolver";

        protected override void InitializeInternal() {
            //find method
            _decrypterMethod = Utils.GetMethodsRecursive(ctx.Module).SingleOrDefault(CanBeStringMethod)
                               ?? throw new Exception("Could not find decrypter method");
        }

        protected override void ProcessInternal() {
            //a dictionary to cache all strings
            var dictionary = new Dictionary<int, string>();

            //get the decrypter method in a way in which we can invoke it
            var decrypter = Utils.FindMethod(ctx.Assembly, _decrypterMethod, new[] {typeof(int)}) ??
                            throw new Exception("Couldn't find decrypter method through reflection");

            //store it so we can use it in the stacktrace patch
            Runtime.PatchStackTraceGetMethod.MethodToReplace = decrypter;

            //for every method with a body...
            foreach (var method in Utils.GetMethodsRecursive(ctx.Module)
                .Where(a => a.HasBody && a.Body.HasInstructions)
            ) //.. and every instruction (starting at the second one) ...
                for (var i = 1; i < method.Body.Instructions.Count; i++) {
                    //get this instruction and the previous
                    var prev = method.Body.Instructions[i - 1];
                    var curr = method.Body.Instructions[i];

                    //if they invoke the string decrypter method with an int parameter
                    if (prev.IsLdcI4() && curr.Operand != null && curr.Operand is MethodDef md &&
                        md.MDToken == _decrypterMethod.MDToken) {
                        //get the int parameter, and get the resulting string from either cache or invoking the decrypter method
                        int val = prev.GetLdcI4Value();
                        if (!dictionary.ContainsKey(val))
                            dictionary[val] = (string) decrypter.Invoke(null, new object[] {val});

                        // check if str == .ctor due to eaz using string decryptor to call constructors
                        if (dictionary[val] == ".ctor") continue;

                        //replace the instructions with the string

                        prev.OpCode = OpCodes.Nop;
                        curr.OpCode = OpCodes.Ldstr;
                        curr.Operand = dictionary[val];
                    }
                }
        }

        protected override void CleanupInternal() {
            // nothing to cleanup
        }

        private static bool CanBeStringMethod(MethodDef method) {
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
            if (!method.Body.Instructions.Any(a =>
                a.OpCode.Code == dnlib.DotNet.Emit.Code.Call && a.Operand is MethodDef m
                                                             && m.MethodSig.ToString() ==
                                                             "System.String (System.Int32,System.Boolean)")
            )
                return false;

            //is not private or public
            if (method.IsPrivate || method.IsPublic)
                return false;

            return true;
        }
    }
}