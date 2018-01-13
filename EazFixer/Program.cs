using System;
using System.IO;
using System.Linq;
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

            var ctx = new EazContext(file, new ProcessorBase[] {new StringFixer(), new ResourceResolver()});

            Console.WriteLine("Executing memory patches...");
            Harmony.Patch();

            Console.WriteLine("Initializing modules...");
            foreach (ProcessorBase proc in ctx)
                proc.Initialize(ctx);

            Console.WriteLine("Processing...");
            foreach (ProcessorBase proc in ctx.Where(a => a.Initialized))
                proc.Process();

            Console.WriteLine("Cleanup...");
            foreach (ProcessorBase proc in ctx.Where(a => a.Processed))
                proc.Cleanup();

            //write success/failure
            Console.WriteLine();
            Console.WriteLine("Applied patches:");
            var cc = Console.ForegroundColor;
            foreach (ProcessorBase p in ctx) {
                Console.Write(p.GetType().Name + ": ");
                if (p.CleanedUp) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Success");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed ({p.ErrorMessage})");
                }
                Console.ForegroundColor = cc;
            }
            Console.WriteLine();

            Console.WriteLine("Writing new assembly...");
            string path = Path.Combine(Path.GetDirectoryName(file) ?? Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(file) + "-eazfix" + Path.GetExtension(file));
            ctx.Module.Write(path);

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
