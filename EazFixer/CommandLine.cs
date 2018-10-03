using System;
using System.Collections.Generic;
using System.Linq;

namespace EazFixer {
    internal class CommandLine
    {
        public static bool Parse(string[] args, ref CommandLineOption[] options)
        {
            if (args.Length == 0) return false;

            var argt = 0; //temp (1-2)
            var argn = string.Empty;

            var usedArgs = new List<string>();
            var usedArgsValues = new List<object>();

            foreach (var item in args)
            {
                if (argt > 1) argt = 0;

                //if param is new param and not value
                if (item.StartsWith("--") && argt > 1)
                {
                    argt = 0;
                    usedArgs.Add(item);
                    usedArgsValues.Add(null);
                    continue;
                }
                
                argt++;
                
                if (argt == 1) argn = item;

                //if arg is already set
                if (usedArgs.Contains(argn) && argt == 1)
                    throw new Exception($"Parameter {argn} is already defined");

                //check if new arg or setting value
                //and add results to [x] list
                if (argt == 1) usedArgs.Add(argn);
                else usedArgsValues.Add(item);
            }
            
            //set values
            options = usedArgs.Select((t, i) => new CommandLineOption(t, usedArgsValues.Count > i ? usedArgsValues[i] : null)).ToArray();
            return true;
        }

        public struct CommandLineOption
        {
            public readonly string Name;
            public readonly object Value;

            public CommandLineOption(string name, object value)
            {
                this.Name = name;
                this.Value = value;
            }
        }
    }
}