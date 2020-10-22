using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ROOT.Shared.Utils.OS
{
    public static class SSH
    {
        const string WindowsSSh = "C:\\Windows\\System32\\OpenSSH\\ssh.exe";
        private const string UnixSsh = "/usr/bin/ssh";


        public static string BinPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsSSh : UnixSsh;
    }


    public class RemoteProcessCall : ProcessCall
    {
        public RemoteProcessCall(string username, string hostName)
            : base(SSH.BinPath, $"{username}@{hostName}")
        {
        }

        public static ProcessCall operator |(RemoteProcessCall first, ProcessCall second)
        {
            return first.Pipe(second);
        }
    }
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
                return new ProcessCallResult { ExitCode = exitCode, StdOut = process.StandardOutput.ReadToEnd(), StdError = process.StandardError.ReadToEnd(), CommandLine = string.Join(" ", BinPath, args) };
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
            return first.Pipe(second);
        }
    }

    public static class ProcessCallExtensions
    {
        const string WindowsShell = "C:\\Windows\\System32\\cmd.exe";
        private const string UnixShell = "/bin/bash";

        private static readonly string Shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsShell : UnixShell;

        public static ProcessCall Pipe(this ProcessCall processCall, ProcessCall other)
        {
            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            string args = string.Concat("/c", " ", processCall.BinPath, " ", processCall.Arguments, " | ", other.BinPath, " ", other.Arguments).Trim();

            return new ProcessCall(Shell, args);
        }

        public static ProcessCall Pipe(this RemoteProcessCall processCall, ProcessCall other)
        {
            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            string args = string.Concat("/c", " ", processCall.BinPath, " ", processCall.Arguments, " \"", other.BinPath, " ", other.Arguments, "\"").Trim();

            return new ProcessCall(Shell, args);
        }

        public static ProcessCallException ToException(this ProcessCallResult result)
        {
            return new ProcessCallException(result.CommandLine, result.ExitCode, result.StdOut, result.StdError);
        }
    }

    [Serializable]
    public class ProcessCallException : Exception
    {
        public string CommandLine { get; }
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdError { get; }

        public ProcessCallException(string commandLine, int exitCode, string stdOut, string stdError)
            : base($"Command: {commandLine} failed with error code: " + exitCode + Environment.NewLine + stdError)
        {
            CommandLine = commandLine;
            ExitCode = exitCode;
            StdOut = stdOut;
            StdError = stdError;
        }
    }

    public class ProcessCallResult
    {
        public bool Success => ExitCode == 0;
        public int ExitCode { get; set; }
        public string StdOut { get; set; }
        public string StdError { get; set; }
        public string CommandLine { get; set; }
    }
}