using System;

namespace MakeFunctionJson
{
    internal class ConsoleLogger : ILogger
    {
        public void LogError(string message)
        {
            Console.Error.WriteLine(message);
        }

        public void LogErrorFromException(Exception e)
        {
            Console.Error.WriteLine(e);
        }

        public void LogWarningFromException(Exception e)
        {
            Console.WriteLine(e);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine(message);
        }
    }
}