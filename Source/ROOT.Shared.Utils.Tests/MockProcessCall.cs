using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests;

internal class MockProcessCall : IProcessCall
{
    public string StdOutput { get; set; }

    public string StdError { get; set; }

    public ProcessCallResult LoadResponse(bool throwOnFailure, params string[] arguments)
    {
        return LoadResponseAsync(throwOnFailure, new ProcessCallArguments(arguments)).GetAwaiter().GetResult();
    }

    public ProcessCallResult LoadResponse(bool throwOnFailure, Stream inputStream, params string[] arguments)
    {
        return LoadResponseAsync(throwOnFailure, inputStream, new ProcessCallArguments(arguments)).GetAwaiter().GetResult();
    }

    public Task<ProcessCallResult> LoadResponseAsync(bool throwOnFailure, ProcessCallArguments arguments = default, CancellationToken cancellationToken = default)
    {
        return LoadResponseAsync(throwOnFailure, null, arguments, cancellationToken);
    }

    public Task<ProcessCallResult> LoadResponseAsync(bool throwOnFailure, Stream inputStream, ProcessCallArguments arguments = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
        return Task.FromResult(res);
    }

    public bool Success { get; set; } = true;
    public string BinPath { get; set; }
    public string Arguments { get; set; }
    public string FullCommandLine { get; set; }
    public string Shell { get; set; }
    public bool UseShell { get; set; }
    public TimeSpan Timeout { get; set; }
    public bool Started { get; set; }
    public bool RequiresSudo { get; set; }

    public IProcessCall Pipe(IProcessCall other)
    {
        return this;
    }
}
