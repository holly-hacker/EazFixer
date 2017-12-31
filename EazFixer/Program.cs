using System;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using EazFixer.Processors;

namespace EazFixer
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            string file;
            if (args.Length != 1 || !File.Exists(file = args[0]))
                return Exit("Please give me a file", true);

            ProcessorBase[] procList = {new StringFixer(), new ResourceResolver()};

            ModuleDefMD mod = ModuleDefMD.Load(file);
            Assembly asm = Assembly.LoadFile(file);

            Console.WriteLine("Executing memory patches...");
            Harmony.Patch();

            Console.WriteLine("Initializing modules...");
            foreach (ProcessorBase proc in procList)
                proc.Initialize(mod);

            Console.WriteLine("Processing...");
            foreach (ProcessorBase proc in procList.Where(a => a.Initialized))
                proc.Process(mod, asm);

            Console.WriteLine("Cleanup...");
            foreach (ProcessorBase proc in procList.Where(a => a.Success))
                proc.Cleanup(mod);

            //write success/failure
            Console.WriteLine();
            Console.WriteLine("Applied patches:");
            var cc = Console.ForegroundColor;
            foreach (var p in procList)
            {
                Console.Write(p.GetType().Name + ": ");
                Console.ForegroundColor = p.Success ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine(p.Success ? "Success" : "Failed");
                Console.ForegroundColor = cc;
            }
            Console.WriteLine();

            Console.WriteLine("Writing new assembly...");
            string path = Path.Combine(Path.GetDirectoryName(file) ?? Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(file) + "-eazfix" + Path.GetExtension(file));
            mod.Write(path);

#if DEBUG
            return Exit("DONE", true);
#else
            return Exit("Done.");
#endif
        }

        private static int Exit(string reason, bool askForInput = false)
        {
            Console.WriteLine(reason);
            if (askForInput) {
                Console.Write("Press any key to exit... ");
                Console.ReadKey();
            }
            return 0;
        }
    }
}
