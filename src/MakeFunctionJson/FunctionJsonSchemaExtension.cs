using System.IO;
using Newtonsoft.Json;

namespace MakeFunctionJson
{
    internal static class FunctionJsonSchemaExtension
    {
        public static void Serialize(this FunctionJsonSchema functionJson, string path)
        {
            var content = JsonConvert.SerializeObject(functionJson, Formatting.Indented);
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, content);
        }
    }
}
