using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EazFixer.Processors
{
    internal class StringFixer : IProcessor
    {
        private readonly string _file;
        private MethodDef _decrypterMethod;
        private Dictionary<int, string> _dictionary = new Dictionary<int, string>();

        public StringFixer(string file) => _file = file;

        public void PreProcess(ModuleDef m)
        {
            //find method
            _decrypterMethod = Utils.GetMethodsRecursive(m).Single(CanBeStringMethod);
        }

        public void Process(ModuleDef m)
        {
            //load it normally and get all strings by invoking
            var decrypter = FindMethod(Assembly.LoadFile(_file), _decrypterMethod) ?? throw new Exception("Couldn't find decrypter type again");

            //for every method with a body
            foreach (MethodDef meth in Utils.GetMethodsRecursive(m).Where(a => a.HasBody && a.Body.HasInstructions))
            {
                //we start at 1, so there always is an instruction before us
                for (int i = 1; i < meth.Body.Instructions.Count; i++)
                {
                    var prev = meth.Body.Instructions[i - 1];
                    var curr = meth.Body.Instructions[i];
                    
                    if (prev.IsLdcI4() && curr.Operand != null && curr.Operand is MethodDef md && new SigComparer().Equals(md, _decrypterMethod))
                    {
                        int val = prev.GetLdcI4Value();
                        _dictionary[val] = (string) decrypter.Invoke(null, new object[] {val});
                    }
                }
            }
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

        private static MethodInfo FindMethod(Assembly ass, MethodDef meth)
        {
            Type type = ass.GetType(meth.DeclaringType.ReflectionFullName);
            return type.GetMethod(meth.Name, BindingFlags.Static | BindingFlags.NonPublic, null, new[] {typeof(int)}, null);
        }
    }
}
