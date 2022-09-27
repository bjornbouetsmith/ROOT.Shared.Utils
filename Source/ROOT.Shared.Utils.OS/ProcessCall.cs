using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ROOT.Shared.Utils.OS
{
    public class ProcessCall
    {
        public string BinPath { get; }
        public string Arguments { get; }
        public string FullCommandLine { get; }
        public string Shell { get; set; }
        public bool UseShell { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        public ProcessCall(string binPath, string arguments = "")
        {
            BinPath = binPath;
            Arguments = arguments;
            if (arguments == "")
            {
                FullCommandLine = binPath;
            }
            else
            {
                FullCommandLine = binPath + " " + arguments;
            }
        }

        public ProcessCallResult LoadResponse(params string[] arguments)
        {
            return LoadResponse(null, arguments);
        }

        public ProcessCallResult LoadResponse(Stream inputStream, params string[] arguments)
        {
            var exec = this.Execute();
            Started = true;
            string args = exec.Arguments;
            if (arguments != null && arguments.Length > 0)
            {
                args += " " + string.Join(" ", arguments);
            }

            args = args.Trim();

            var startInfo = new ProcessStartInfo(exec.BinPath, args)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,

            };

            using var process = new Process { StartInfo = startInfo };
            using var reader = new ProcessReader(process, startInfo, Timeout, inputStream);


            reader.Wait();
            Trace.TraceInformation($"Command: {FullCommandLine} success");
            var exitCode = reader.ExitCode;

            var stdOut = reader.Output?.ReadToEnd();
            var stdError = reader.Error?.ReadToEnd();

            return new ProcessCallResult { ExitCode = exitCode, StdOut = stdOut, StdError = stdError, CommandLine = string.Join(" ", exec.BinPath, args) };
        }

        private void Write(List<string> content, string what)
        {
            lock (content)
            {
                content.Add(what);
            }
        }

        public bool Started { get; private set; }

        private static void CopyStreamToProcessInput(Stream inputStream, Process process)
        {
            if (inputStream != null)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = inputStream.Read(buffer, 0, 1024);
                while (bytesRead > 0)
                {
                    process.StandardInput.BaseStream.Write(buffer, 0, bytesRead);
                    bytesRead = inputStream.Read(buffer, 0, 1024);
                }
            }
        }

        public static ProcessCall operator |(ProcessCall first, ProcessCall second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }
            return first.Pipe(second);
        }
    }
}