using System;
using System.IO;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests
{
    internal class MockProcessCall : IProcessCall
    {
        public ProcessCallResult LoadResponse(params string[] arguments)
        {
            return new ProcessCallResult
            {
                CommandLine = FullCommandLine,
                ExitCode = Success ? 0 : 1,
                StdError = StdError,
                StdOut = StdOutput
            };
        }

        public string StdOutput { get; set; }

        public string StdError { get; set; }

        public ProcessCallResult LoadResponse(Stream inputStream, params string[] arguments)
        {
            return new ProcessCallResult
            {
                CommandLine = FullCommandLine,
                ExitCode = Success ? 0 : 1,
                StdError = StdError,
                StdOut = StdOutput
            };
        }

        public bool Success { get; set; } = true;
        public string BinPath { get; set; }
        public string Arguments { get; set; }
        public string FullCommandLine { get; set; }
        public string Shell { get; set; }
        public bool UseShell { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool Started { get; set; }
        public IProcessCall Pipe(IProcessCall other)
        {
            return this;
        }
    }
}
