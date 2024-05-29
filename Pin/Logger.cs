using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pin {
    class Logger {
        private string prefix;

        public static Logger App {
            get => new Logger("APP");
        }

        public static Logger MyPos {
            get => new Logger("MyPOS");
        }

        private string msgPrefix {
            get => $"{DateTime.Now.ToString()} [{prefix}]";
        }

        public Logger(string prefix) {
            this.prefix = prefix;
        }

        public static void StopSpinner() {
            ConsoleSpinner.Stop();
        }

        public void Log(string message) {
            Console.WriteLine($"{msgPrefix} {message}");
        }

        public void LogWithSpinner(string message) {
            Console.Write($"{msgPrefix} {message} ");
            _ = ConsoleSpinner.Start();
        }

        public void Error(string message) {
            LogWithColor(message, ConsoleColor.Red);
        }

        public void Warn(string message) {
            LogWithColor(message, ConsoleColor.Yellow);
        }

        public void Info(string message) {
            LogWithColor(message, ConsoleColor.Blue);
        }

        public void Success(string message) {
            LogWithColor($"{message}", ConsoleColor.Green);
        }

        private void LogWithColor(string message, ConsoleColor color) {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.Write($"{msgPrefix} ");
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = currentColor;
            Console.Write("\n");
        }
    }
}
