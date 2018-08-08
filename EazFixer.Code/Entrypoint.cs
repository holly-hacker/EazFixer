using System.IO;
using System.Linq;
using EazFixer.Code.Other;
using EazFixer.Code.Processors;

namespace EazFixer.Code {
    public class Entrypoint {
        public static int Start(string[] args) {
            Logger.Initialize("EazFixerLog.md", "[~]");

            var options = new Options();

            options.inFile = string.Empty;
            options.outFile = string.Empty;

            foreach (var item in CommandLine.Parse(args))
                switch (item.name) {
                    case "-f":
                        options.inFile = item.value.ToString();
                        break;

                    case "-o":
                        options.outFile = item.value.ToString();
                        break;
                }

            if (!File.Exists(options.inFile)) {
                Logger.Error($"file {options.inFile} does not exist!");
                return 0;
            }

            if (options.outFile == string.Empty)
                options.outFile = Path.GetFileNameWithoutExtension(options.inFile) + "-eazfix" +
                                  Path.GetExtension(options.inFile);

            //order is important! AssemblyResolver has to be after StringFixer and ResourceResolver

            var processorBases = new ProcessorBase[] {
                new StringResolver(),
                new ResourceResolver(),
                new AssemblyResolver()
            };

            var ctx = new EazContext(options.inFile, processorBases);

            Logger.Alert("Executing memory patches.");
            Runtime.Patch();

            Logger.Alert("Initializing file patches.");
            foreach (var processorBase in ctx)
                processorBase.Initialize(ctx);

            Logger.Alert("Executing patches.");
            foreach (var processorBase in ctx.Where(a => a.Initialized))
                processorBase.Process();

            Logger.Alert("Cleaning up.");
            foreach (var processorBase in ctx.Where(a => a.Processed))
                processorBase.Cleanup();

            foreach (var processorBase in ctx) {
                if (processorBase.CleanedUp) {
                    Logger.Alert($"Success [({processorBase.ProcessorName})]");
                    continue;
                }

                Logger.Error($"Failed  [({processorBase.ProcessorName}, {processorBase.ErrorMessage})]");
            }

            Logger.Alert($"Saving assembly [({options.outFile})]");
            ctx.Module.Write(options.outFile);


            return Exit("Finished.");
        }

        private static int Exit(string message) {
            Logger.Alert(message);
            return 0;
        }

        private struct Options {
            public string inFile;
            public string outFile;
        }
    }
}