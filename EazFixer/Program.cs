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
            try
            {
                OptionsParser.SetFlags(args);

                // Determine the output path if not given
                Flags.OutFile = Path.Combine(Path.GetDirectoryName(Flags.InFile) ?? "", Path.GetFileNameWithoutExtension(Flags.InFile) + "-eazfix" + Path.GetExtension(Flags.InFile));

                //order is important! AssemblyResolver has to be after StringFixer and ResourceResolver
                var ctx = new EazContext(!string.IsNullOrEmpty(Flags.InFile) ? Flags.InFile : throw new Exception("Filepath not defined!"), 
                    new ProcessorBase[] {new StringFixer(), new ResourceResolver(), new AssemblyResolver()});

                Console.WriteLine("Executing memory patches...");
                StacktracePatcher.Patch();

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
                foreach (ProcessorBase p in ctx)
                {
                    Console.Write(p.GetType().Name + ": ");

                    if (p.CleanedUp)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Success");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed ({p.ErrorMessage})");
                    }

                    Console.ForegroundColor = cc;
                }

                Console.WriteLine();

                Console.WriteLine("Writing new assembly...");
                ctx.Module.Write(Flags.OutFile);

#if DEBUG
                return Exit("DONE", true);
#else
                return Exit("Done.");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                return 0;
            }
        }

        private static int Exit(string reason, bool askForInput = false)
        {
            Console.WriteLine(reason);
            if (askForInput)
            {
                Console.Write("Press any key to exit... ");
                Console.ReadKey();
            }
            return 0;
        }
    }
}
