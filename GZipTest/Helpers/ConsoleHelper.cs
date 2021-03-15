using System;

namespace GZipTest.Helpers
{
    internal class ConsoleHelper
    {
        public static void WriteErrorMessage(string errorMessage)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {errorMessage}");
            Console.ForegroundColor = oldColor;
        }

        public static void WriteInfoMessage(string infoMessage)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"INFO: {infoMessage}");
            Console.ForegroundColor = oldColor;
        }

        public static void WriteProcessMessage(string processMessage)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"PROCESS: {processMessage}");
            Console.ForegroundColor = oldColor;
        }
    }
}
