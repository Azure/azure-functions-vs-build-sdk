using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MakeFunctionJson
{
    internal class FunctionJsonSchema
    {
        [JsonProperty("bindings")]
        public IEnumerable<JObject> Bindings { get; set; }

        [JsonProperty("disabled")]
        public object Disabled { get; set; }

        [JsonProperty("scriptFile")]
        public string ScriptFile { get; set; }

        [JsonProperty("entryPoint")]
        public string EntryPoint { get; set; }
    }

    internal enum Direction
    {
        @in,
        @out,
        @inout
    }
}
