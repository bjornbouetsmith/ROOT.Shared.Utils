using System;
using System.Runtime.InteropServices;

namespace ROOT.Shared.Utils.OS
{
    public static class ProcessCallExtensions
    {
        public const string WindowsShell = "C:\\Windows\\System32\\cmd.exe";
        public const string UnixShell = "/bin/bash";

        private static readonly string Shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsShell : UnixShell;

        public static IProcessCall ForceShell(this IProcessCall processCall, OSPlatform platform)
        {
            var shell = platform == OSPlatform.Windows ? WindowsShell : UnixShell;
            return new ProcessCall(processCall.BinPath, processCall.Arguments) { Shell = shell };
        }

        public static IProcessCall Pipe(this IProcessCall processCall, IProcessCall other)
        {
            if (processCall is SSHProcessCall remote)
            {
                return Pipe(remote, other);
            }

            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            return processCall.Pipe(other);
        }

        public static ProcessCall Pipe(this ProcessCall processCall, ProcessCall other)
        {
            if (processCall is SSHProcessCall remote)
            {
                return Pipe(remote, (IProcessCall)other);
            }

            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            string args = string.Concat(processCall.Arguments, " | ", other.BinPath, " ", other.Arguments).Trim();
            return new ProcessCall(processCall.BinPath, args);
        }

        public static ProcessCall Pipe(this SSHProcessCall processCall, IProcessCall other)
        {
            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            string args = string.Concat(processCall.Arguments, " \"", processCall.RequiresSudo ? Sudo.BinPath : "", " ", other.BinPath, " ", other.Arguments, "\"").Trim();

            return new ProcessCall(processCall.BinPath, args);
        }

        public static IProcessCall Execute(this IProcessCall processCall)
        {
            if (processCall.Started)
            {
                throw new InvalidOperationException("Cannot execute  process that has been started");
            }

            if (!processCall.UseShell)
            {
                return processCall;
            }
            string args = string.Concat(ShellPrefix, " \"", processCall.BinPath, " ", processCall.Arguments, "\"").Trim();

            var shell = processCall.Shell ?? Shell;
            return new ProcessCall(shell, args);
        }

        public static ProcessCallException ToException(this ProcessCallResult result)
        {
            return new ProcessCallException(result.CommandLine, result.ExitCode, result.StdOut, result.StdError);
        }

        public const string WindowsShellPrefix = "/Q /C";
        public const string UnixShellPrefix = "-c";
        public static string ShellPrefix { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsShellPrefix : UnixShellPrefix;
    }
}