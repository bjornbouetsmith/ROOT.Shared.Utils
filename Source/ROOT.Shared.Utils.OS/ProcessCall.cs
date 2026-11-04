using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ROOT.Shared.Utils.OS;

public partial class ProcessCall : IProcessCall
{
    public string BinPath { get; }
    public string Arguments { get; }
    public string FullCommandLine { get; }
    public string Shell { get; set; }
    public bool UseShell { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public bool RequiresSudo { get; set; }

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

    public ProcessCallResult LoadResponse(bool throwOnFailure, params string[] arguments)
    {
        return LoadResponseAsync(throwOnFailure,new ProcessCallArguments(arguments)).GetAwaiter().GetResult();
    }

    public ProcessCallResult LoadResponse(bool throwOnFailure, Stream inputStream, params string[] arguments)
    {
        return LoadResponseAsync(throwOnFailure, inputStream, new ProcessCallArguments(arguments)).GetAwaiter().GetResult();
    }

    public Task<ProcessCallResult> LoadResponseAsync(bool throwOnFailure, ProcessCallArguments arguments = default, CancellationToken cancellationToken = default)
    {
        return LoadResponseAsync(throwOnFailure, null, arguments, cancellationToken);
    }

    public async Task<ProcessCallResult> LoadResponseAsync(bool throwOnFailure, Stream inputStream, ProcessCallArguments arguments = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exec = this.Execute();
        Started = true;
        string args = exec.Arguments;

        if (arguments.Arguments is { Length: > 0 } processArguments)
        {
            args += " " + string.Join(" ", processArguments);
        }

        args = args.Trim();

        var startInfo = new ProcessStartInfo(exec.BinPath, args)
        {
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,

        };
        
        using var process = new Process ();
        process.StartInfo = startInfo;

        using var reader = new ProcessReader(process, Timeout, inputStream);

        using var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancelSource.CancelAfter(Timeout);

        try
        {
            await reader.WaitAsync(cancelSource.Token).ConfigureAwait(false);
        }
        catch (TimeoutException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        Trace.TraceInformation($"Command: {FullCommandLine} success");
        var exitCode = reader.ExitCode;

        var stdOut = reader.Output is null ? null : await reader.Output.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stdError = reader.Error is null ? null : await reader.Error.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var result = new ProcessCallResult { ExitCode = exitCode, StdOut = stdOut, StdError = stdError, CommandLine = string.Join(" ", exec.BinPath, args) };
        if (throwOnFailure && exitCode != 0)
        {
            throw result.ToException();
        }

        return result;
    }

    public bool Started { get; private set; }
    public IProcessCall Pipe(IProcessCall other)
    {
        return this | other;
    }
}