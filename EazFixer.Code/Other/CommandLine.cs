using System;
using System.Collections.Generic;
using System.Linq;

namespace EazFixer.Code.Other {
    internal class CommandLine {
        private static void ShowHelp() {
            Logger.Alert("to use pass command line args or drag and drop file");
            Logger.Alert("valid options are: -f [INFILE] -o [OUTFILE] -h [HELPSCREEN]");
            Logger.Alert("EazFixer.exe -f obfuscated.exe [(-o is optional for custom out)]");
            Console.ResetColor();
            Environment.Exit(0);
        }

        public static CommandLineOption[] Parse(string[] args) {
            if (args.Contains("-h") || args.Length == 0) ShowHelp();

            var argc = args.Length;
            var argt = 0; // temp (1-2)

            var argn = string.Empty;

            var usedArgs = new List<string>();
            var usedArgsValues = new List<string>();
            var commandLineOptions = new List<CommandLineOption>();

            foreach (var item in args) {
                if (argt == 2) argt = 0;

                argc++;
                argt++;

                if (argt == 1) argn = item;

                if (usedArgs.Contains(argn) && argt == 1) throw new Exception($"parameter {argn} is already defined");
                if (item.StartsWith("-") && argt == 2) throw new Exception($"parameter {argn} is not assigned a value");

                // check if new arg or setting value
                // and add results to [x] list
                if (argt == 1) usedArgs.Add(argn);
                else usedArgsValues.Add(item);
            }

            if (argt != 2) throw new Exception($"parameter {argn} is not assigned a value.");

            for (var i = 0; i < usedArgs.Count; i++)
                commandLineOptions.Add(new CommandLineOption(usedArgs[i], usedArgsValues[i]));

            return commandLineOptions.ToArray();
        }

        public struct CommandLineOption {
            public readonly string name;
            public readonly object value;

            public CommandLineOption(string name, object value) {
                this.name = name;
                this.value = value;
            }
        }
    }
}