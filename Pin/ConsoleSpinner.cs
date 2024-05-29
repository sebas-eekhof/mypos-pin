using System;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pin {
    public class ConsoleSpinner {
        private static int counter = 0;
        private static bool started = false;
        private static int spinnerLeft = 0;
        private static int spinnerTop = 0;

        public static async Task Start() {
            if(started || Console.CursorLeft == 0) return;
            started = true;
            spinnerLeft = Console.CursorLeft;
            spinnerTop = Console.CursorTop;
            Console.CursorVisible = false;
            while(started) {
                counter++;
                Console.SetCursorPosition(spinnerLeft, spinnerTop);
                switch(counter % 4) {
                    case 0: Console.Write("/"); break;
                    case 1: Console.Write("-"); break;
                    case 2: Console.Write("\\"); break;
                    case 3: Console.Write("|"); break;
                }
                await Task.Delay(100);
            }
        }

        public static void Stop() {
            if(!started) return;
            started = false;
            Console.SetCursorPosition(spinnerLeft, spinnerTop);
            Console.Write("  \n");
            Console.CursorVisible = true;
        }
    }
}
