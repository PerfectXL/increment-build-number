using System;
using System.Runtime.InteropServices;

namespace IncrementBuildNumber
{
    internal class ConsoleHelper
    {
        public static void PauseIfRequired()
        {
            Console.WriteLine();
            if (!ConsoleWillBeDestroyedAtTheEnd())
            {
                return;
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }

        private static bool ConsoleWillBeDestroyedAtTheEnd()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);
            return processCount == 1;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
    }
}