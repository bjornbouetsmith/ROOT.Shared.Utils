using System;

namespace ROOT.Shared.Utils.OS
{
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
}