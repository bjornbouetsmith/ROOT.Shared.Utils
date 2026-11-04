using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

        var processArguments = arguments.Arguments;
        ProcessCall callToExecute = this;

        if (processArguments.Length > 0)
        {
            if (Started)
            {
                throw new InvalidOperationException("Cannot execute process that has been started");
            }

            callToExecute = new ProcessCall(BinPath, AppendArguments(Arguments, processArguments, UseShell))
            {
                Shell = Shell,
                UseShell = UseShell,
                Timeout = Timeout,
                RequiresSudo = RequiresSudo,
            };
        }

        var exec = callToExecute.Execute();
        string args = exec.Arguments.Trim();

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

        try
        {
            await reader.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // The instance is only marked as started once the process was actually launched.
            if (reader.Started)
            {
                Started = true;
            }
        }

        var exitCode = reader.ExitCode;

        var stdOut = reader.Output is null ? null : await reader.Output.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stdError = reader.Error is null ? null : await reader.Error.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var result = new ProcessCallResult { ExitCode = exitCode, StdOut = stdOut, StdError = stdError, CommandLine = string.Join(" ", exec.BinPath, args) };
        if (throwOnFailure && exitCode != 0)
        {
            throw result.ToException();
        }

        if (exitCode == 0)
        {
            Trace.TraceInformation($"Command: {FullCommandLine} success");
        }

        return result;
    }

    private static string AppendArguments(string arguments, string[] extraArguments, bool forShell)
    {
        var combined = arguments ?? string.Empty;

        foreach (var argument in extraArguments)
        {
            var quoted = QuoteArgument(argument, forShell);
            combined = combined.Length == 0 ? quoted : combined + " " + quoted;
        }

        return combined;
    }

    private static string QuoteArgument(string argument, bool forShell)
    {
        if (argument.Length == 0)
        {
            return "\"\"";
        }

        if (!NeedsQuoting(argument, forShell))
        {
            return argument;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return QuoteWindowsArgument(argument);
        }

        if (forShell)
        {
            return "'" + argument.Replace("'", "'\\''") + "'";
        }

        return QuoteWindowsArgument(argument);
    }

    private static bool NeedsQuoting(string argument, bool forShell)
    {
        foreach (var c in argument)
        {
            if (char.IsWhiteSpace(c) || c == '"' || c == '\'')
            {
                return true;
            }

            if (forShell && c is '$' or '`' or '&' or '|' or '<' or '>' or ';' or '(' or ')' or '^' or '%' or '!' or '*' or '?')
            {
                return true;
            }
        }

        return false;
    }

    private static string QuoteWindowsArgument(string argument)
    {
        var result = new StringBuilder();
        result.Append('"');

        var backslashes = 0;
        foreach (var c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            if (c == '"')
            {
                result.Append('\\', backslashes * 2 + 1);
                result.Append('"');
                backslashes = 0;
                continue;
            }

            result.Append('\\', backslashes);
            backslashes = 0;
            result.Append(c);
        }

        result.Append('\\', backslashes * 2);
        result.Append('"');
        return result.ToString();
    }

    public bool Started { get; private set; }
    public IProcessCall Pipe(IProcessCall other)
    {
        return this | other;
    }
}