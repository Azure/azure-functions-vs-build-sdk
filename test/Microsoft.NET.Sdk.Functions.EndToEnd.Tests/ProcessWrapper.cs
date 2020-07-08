﻿using System;
using System.Diagnostics;
using System.IO;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Functions.EndToEnd.Tests
{
    public class ProcessWrapper
    {
        public int? RunProcess(string fileName, string arguments, string workingDirectory, out int? processId, bool createDirectoryIfNotExists = true, bool waitForExit = true, ITestOutputHelper testOutputHelper = null)
        {
            if (createDirectoryIfNotExists && !Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                FileName = fileName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            if (!string.IsNullOrEmpty(arguments))
            {
                startInfo.Arguments = arguments;
            }

            Process testProcess = Process.Start(startInfo);
            processId = testProcess?.Id;
            if (waitForExit)
            {
                testProcess.WaitForExit(3 * 60 * 1000);
                var standardOut = testProcess.StandardOutput.ReadToEnd();
                var standardError = testProcess.StandardError.ReadToEnd();
                testOutputHelper?.WriteLine(standardOut);
                testOutputHelper?.WriteLine(standardError);
                return testProcess?.ExitCode;
            }

            return -1;
        }

        public static void KillProcessTree(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                if (process != null && !process.HasExited)
                {
                    KillProcessTreeInternal(processId);
                }
            }
            catch (Exception)
            {
            }

        }

        private static void KillProcessTreeInternal(int pid)
        {
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // vramak: Process might have already exited.
            }
        }
    }
}
