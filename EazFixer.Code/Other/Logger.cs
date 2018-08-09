using System;
using System.IO;

namespace EazFixer.Code.Other {
    internal class Logger {
        private static StreamWriter stream_;
        private static string prefix_;

        public static void Initialize(string filepath, string prefix) {
            try {
                prefix_ = prefix + ' ';
                stream_ = new StreamWriter(filepath, true);
                stream_.AutoFlush = true;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(prefix_ + ex.Message);
                Environment.Exit(0);
            }
        }

        public static void Alert(string message) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(prefix_ + message);
            stream_.WriteLine(prefix_ + message);
        }

        public static void Message(string message) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(prefix_ + message);
            stream_.WriteLine(prefix_ + message);
        }

        public static void Error(string message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(prefix_ + message);
            stream_.WriteLine(prefix_ + message);
        }
    }
}