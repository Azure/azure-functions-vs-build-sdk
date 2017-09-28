using System;

namespace MakeFunctionJson
{
    internal static class Logger
    {
        public static void LogError(string message)
        {
            Console.Error.WriteLine(message);
        }

        public static void LogErrorFromException(Exception e)
        {
            Console.Error.WriteLine(e);
        }

        public static void LogWarningFromException(Exception e)
        {
            Console.WriteLine(e);
        }

        public static void LogWarning(string message)
        {
            Console.WriteLine(message);
        }
    }
}