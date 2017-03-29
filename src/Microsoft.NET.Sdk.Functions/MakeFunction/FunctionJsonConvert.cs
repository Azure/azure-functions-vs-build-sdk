using System;

namespace MakeFunctionJson
{
    public static class FunctionJsonConvert
    {
        public static void Convert(string assemblyPath, string outputPath)
        {
            var converter = new FunctionJsonConverter(assemblyPath, outputPath);
            converter.Run();
        }
    }
}
