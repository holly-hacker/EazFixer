using System;
using System.IO;
using System.Linq;

namespace EazFixer {
    public static class OptionsParser
    {
        public static void SetFlags(string[] args)
        {
            var providedArgs = args.Where(x => x.StartsWith("--")).ToList();

            if (args.Length < 2 || !providedArgs.Contains("--file"))
                throw new FormatException();

            foreach(string option in providedArgs)
            {
                switch (option)
                {
                    case "--file":
                        string path = args[Array.IndexOf(args, option) + 1];

                        if (!char.IsLetter(path[0]))
                            throw new FormatException();

                        Flags.InFile = Path.GetFullPath(path);
                        Console.WriteLine(path);
                        break;

                    case "--virt-fix":
                        Flags.VirtFix = true;
                        Console.WriteLine(nameof(Flags.VirtFix));
                        break;

                    case "--keep-types":
                        Flags.KeepTypes = true;
                        Console.WriteLine(nameof(Flags.KeepTypes));
                        break;

                    default: break;
                };
            }
        }
    }
}