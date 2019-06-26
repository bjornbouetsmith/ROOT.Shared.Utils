using System;
using System.Diagnostics;
using System.IO;

namespace ROOT.Shared.Utils.OS
{
    public class ProcessCall
    {
        public string BinPath { get; }
        public string Arguments { get; }
        public string FullCommandLine { get; }

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
            Started = true;
            string args = Arguments;
            if (arguments != null && arguments.Length > 0)
            {
                args = string.Join(" ", arguments);
            }
            var startInfo = new ProcessStartInfo(BinPath, args)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,

            };


            using (var process = Process.Start(startInfo))
            {
                CopyStreamToProcessInput(inputStream, process);

                process.WaitForExit();
                var exitCode = process.ExitCode;
                return new ProcessCallResult { Success = exitCode == 0, StdOut = process.StandardOutput.ReadToEnd(), StdError = process.StandardError.ReadToEnd() };
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
    }

    public static class ProcessCallExtensions
    {
        const string WindowsShell = "C:\\Windows\\System32\\cmd.exe";
        private const string UnixShell = "/bin/bash";

        private static readonly string Shell = Environment.OSVersion.Platform == PlatformID.Win32NT ? WindowsShell : UnixShell;

        public static ProcessCall Pipe(this ProcessCall processCall, ProcessCall other)
        {
            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            string args = string.Concat("/c"," ", processCall.BinPath, " ", processCall.Arguments, " | ", other.BinPath, " ", other.Arguments).Trim();

            return new ProcessCall(Shell, args);
        }
    }

    public class ProcessCallResult
    {
        public bool Success { get; set; }
        public string StdOut { get; set; }
        public string StdError { get; set; }
    }
}