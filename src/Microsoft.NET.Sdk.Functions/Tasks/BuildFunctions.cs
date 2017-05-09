using System.IO;
using MakeFunctionJson;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.Functions.Tasks
{
#if NET46
    [LoadInSeparateAppDomain]
    public class BuildFunctions : AppDomainIsolatedTask
#else
    public class BuildFunctions : Task
#endif
    {
        [Required]
        public string TargetPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public bool GenerateHostJson { get; set; }

        public override bool Execute()
        {
            string hostJsonPath = Path.Combine(OutputPath, "host.json");
            if (GenerateHostJson && !File.Exists(hostJsonPath))
            {
                string hostJsonString = @"{ }";
                File.WriteAllText(hostJsonPath, hostJsonString);
            }

            return FunctionJsonConvert.TryConvert(TargetPath, OutputPath, Log);
        }
    }
}

