using System;
using System.IO;
using System.Linq;
using EazFixer.Processors;

namespace EazFixer
{
    internal class Program
    {
        private static int Main(string[] args) {
            CommandLine.CommandLineOption[] options = null;

            try
            {
                if (args.Length == 1)
                {
                    Flags.InFile = Path.GetFullPath(args[0]);
                    Flags.OutFile = Path.GetFileNameWithoutExtension(Flags.InFile) + "-eazfix" + Path.GetExtension(Flags.InFile);
                }
                else if (CommandLine.Parse(args, ref options))
                {
                    foreach (var opt in options)
                    {
                        if (opt.Name == "--file")
                        {
                            Flags.InFile = (string) opt.Value != string.Empty ? Path.GetFullPath((string) opt.Value) : throw new Exception("Filepath not defined!");
                            Flags.OutFile = Path.GetFileNameWithoutExtension(Flags.InFile) + "-eazfix" + Path.GetExtension(Flags.InFile);
                        }

                        if (opt.Name == "--virt-fix")
                            Flags.VirtFix = true;

                        if (opt.Name == "--keep-types")
                            Flags.KeepTypes = true;
                    }
                }
                else
                {
                    return Exit("Please pass me a file.", true);
                }

                //order is important! AssemblyResolver has to be after StringFixer and ResourceResolver
                var ctx = new EazContext(Flags.InFile != string.Empty ? Flags.InFile : throw new Exception("Filepath not defined!"), new ProcessorBase[] {new StringFixer(), new ResourceResolver(), new AssemblyResolver()});

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
