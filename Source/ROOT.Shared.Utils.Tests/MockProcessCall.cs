using System;
using System.IO;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests
{
    internal class MockProcessCall : IProcessCall
    {
        public ProcessCallResult LoadResponse(bool throwOnFailure, params string[] arguments)
        {
            return LoadResponse(throwOnFailure, null, arguments);
        }

        public string StdOutput { get; set; }

        public string StdError { get; set; }

        public ProcessCallResult LoadResponse(bool throwOnFailure, Stream inputStream, params string[] arguments)
        {
            var res = new ProcessCallResult
            {
                CommandLine = FullCommandLine,
                ExitCode = Success ? 0 : 1,
                StdError = StdError,
                StdOut = StdOutput
            };
            if (throwOnFailure && !Success)
            {
                throw res.ToException();
            }
            return res;
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
