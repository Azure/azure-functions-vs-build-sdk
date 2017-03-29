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

        [Required]
        public string ProjectDirectory { get; set; }

        public override bool Execute()
        {
            bool isSuccess = true;
            Log.LogMessage(MessageImportance.High, $"Building Functions Project");
            FunctionJsonConvert.Convert(TargetPath, Path.Combine(ProjectDirectory, OutputPath));
            return isSuccess;
        }
    }
}

