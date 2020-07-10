using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.Functions.Tasks
{
    public class GenerateFunctions : Task
    {
        [Required]
        public string TargetPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string TaskAssemblyDirectory { get; set; }

        public bool GenerateHostJson { get; set; }

        public ITaskItem[] UserProvidedFunctionJsonFiles { get; set; }

        public bool FunctionsInDependencies { get; set; }

        private const string NETCoreAppFolder = "netcoreapp3.1";

        public override bool Execute()
        {
            string hostJsonPath = Path.Combine(OutputPath, "host.json");
            if (GenerateHostJson && !File.Exists(hostJsonPath))
            {
                string hostJsonString = @"{ }";
                File.WriteAllText(hostJsonPath, hostJsonString);
            }

            string taskDirectoryFullPath = Path.GetFullPath(TaskAssemblyDirectory).TrimEnd(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            string baseDirectory = Path.GetDirectoryName(taskDirectoryFullPath);
            ProcessStartInfo processStartInfo = GetProcessStartInfo(baseDirectory);

            this.Log.LogMessage(MessageImportance.Low, $"Function generator path: '{processStartInfo.FileName}'");
            this.Log.LogCommandLine(MessageImportance.Low, processStartInfo.Arguments);
            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Log.LogWarning(output);
                }

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    Log.LogError(error);
                    Log.LogError("Metadata generation failed.");
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private ProcessStartInfo GetProcessStartInfo(string baseLocation)
        {
            string workingDirectory = Path.Combine(baseLocation, NETCoreAppFolder);
            string exePath = DotNetMuxer.MuxerPathOrDefault();
            string arguments = $"Microsoft.NET.Sdk.Functions.Generator.dll \"{TargetPath} \" \"{OutputPath} \" \"{FunctionsInDependencies} \"";

            string excludedFunctionNamesArg = UserProvidedFunctionJsonFiles?
                    .Select(f => f.ItemSpec.Replace("/", @"\").Replace(@"\function.json", string.Empty))
                    .Where(f => !f.Contains(@"\")) // only first level folders
                    .Aggregate((current, next) => $"{current};{next}");

            if (!string.IsNullOrEmpty(excludedFunctionNamesArg))
            {
                arguments += $" \"{excludedFunctionNamesArg}\"";
            }

            return new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory,
                FileName = exePath,
                Arguments = arguments
            };
        }
    }
}
