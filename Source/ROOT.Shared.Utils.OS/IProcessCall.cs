using System;
using System.IO;

namespace ROOT.Shared.Utils.OS
{
    /// <summary>
    /// Interface for process call, so other implementations are possible, and testing with mocks possible
    /// </summary>
    public interface IProcessCall
    {
        /// <summary>
        /// Loads the response from the process call, using the passed arguments if any
        /// </summary>
        ProcessCallResult LoadResponse(params string[] arguments);
        /// <summary>
        /// Loads the response from the process call, using the passed input stream and arguments if any
        /// </summary>
        ProcessCallResult LoadResponse(Stream inputStream, params string[] arguments);

        string BinPath { get; }
        string Arguments { get; }
        string FullCommandLine { get; }
        string Shell { get; set; }
        bool UseShell { get; set; }
        TimeSpan Timeout { get; set; }
        bool Started { get; }

        public IProcessCall Pipe(IProcessCall other);
    }
}