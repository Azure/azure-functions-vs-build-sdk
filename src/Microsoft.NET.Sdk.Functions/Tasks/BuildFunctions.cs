using System.IO;
using MakeFunctionJson;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.Functions.Tasks
{
    public class BuildFunctions : Task
    {
        [Required]
        public string TargetPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public bool GenerateHostJson { get; set; }

        public override bool Execute()
        {
            bool isSuccess = true;

            if (GenerateHostJson)
            {
                string hostJsonString = @"{ }";
                File.WriteAllText(Path.Combine(OutputPath, "host.json"), hostJsonString);
            }

            FunctionJsonConvert.Convert(TargetPath, OutputPath);
            return isSuccess;
        }
    }
}

