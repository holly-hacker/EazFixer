using System;
using EazFixer.Code;

namespace EazFixer {
    internal class Program {
        private static void Error(string message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[~] {message}");
        }

        private static int Main(string[] args) {
            Console.Title = "EazFixer - Squirrel";

            try {
                Entrypoint.Start(args);
            }
            catch (Exception ex) {
                Error(ex.Message);
            }

            Console.ResetColor();
            return 0;
        }
    }
}