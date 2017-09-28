using System.Diagnostics;
using System.IO;
using System.Reflection;
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

            Assembly taskAssembly = typeof(BuildFunctions).GetTypeInfo().Assembly;
#if NET46
            var info = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName(taskAssembly.Location),
                FileName = Path.Combine(Path.GetDirectoryName(taskAssembly.Location), "Microsoft.NET.Sdk.Functions.Console.exe"),
                Arguments = $"\"{TargetPath}\" \"{OutputPath}\""
            };
#else
            var info = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName(taskAssembly.Location),
                FileName = DotNetMuxer.MuxerPathOrDefault(),
                Arguments = $"Microsoft.NET.Sdk.Functions.Console.dll \"{TargetPath}\" \"{OutputPath}\""
            };
#endif
            using (var process = new Process { StartInfo = info })
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Log.LogWarning(output);

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
    }
}