using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace ROOT.Shared.Utils.OS;

public sealed class ProcessReader : IDisposable
{
    private readonly Process _process;
    private readonly TimeSpan _timeout;
    private readonly Stream _inputStream;
    private readonly StreamWriter _outputWriter;
    private readonly StreamWriter _errorWriter;
    private readonly ManualResetEventSlim _outEvent = new(false);
    private readonly ManualResetEventSlim _errEvent = new(false);
    private readonly SemaphoreSlim _outputLock = new(1);

    public int ExitCode { get; private set; }
    public StreamReader Output { get; private set; }
    public StreamReader Error { get; private set; }

    public ProcessReader(Process process, TimeSpan timeout, Stream inputStream = null)
    {
        if (!process.StartInfo.RedirectStandardError
            && !process.StartInfo.RedirectStandardOutput)
        {
            throw new ArgumentException("Process reader only works with processes that has been started with RedirectStandardOutput or RedirectStandardError", nameof(process));
        }

        if (inputStream != null && !process.StartInfo.RedirectStandardInput)
        {
            throw new ArgumentException($"If {nameof(inputStream)} is not null, then RedirectStandardInput must be true", nameof(process));
        }

        _process = process;

        _timeout = timeout;
        _inputStream = inputStream;
        if (process.StartInfo.RedirectStandardError)
        {
            _errorWriter = new StreamWriter(new MemoryStream(), process.StartInfo.StandardErrorEncoding ?? Encoding.Unicode);
        }

        if (process.StartInfo.RedirectStandardOutput)
        {
            _outputWriter = new StreamWriter(new MemoryStream(), process.StartInfo.StandardInputEncoding ?? Encoding.Unicode);
        }
    }

    private static void CopyStreamToProcessInput(Stream inputStream, Process process)
    {
        if (inputStream == null)
        {
            return;
        }

        using var streamReader = new StreamReader(inputStream);

        process.StandardInput.Write(streamReader.ReadToEnd());
    }

    public async Task WaitAsync(CancellationToken cancellation)
    {
        _process.OutputDataReceived += async (_, e) => await WriteAsync(_outputWriter, e.Data, _outEvent, cancellation);
        _process.ErrorDataReceived += async (_, e) => await WriteAsync(_errorWriter, e.Data, _errEvent, cancellation);
        _process.Start();
        
        // Copy input to process if not nunull
        CopyStreamToProcessInput(_inputStream, _process);
        
        // Start reading output from process
        _process.BeginErrorReadLine();
        _process.BeginOutputReadLine();

        // Wait for process to exit
        await WaitForProcessExit(cancellation).ConfigureAwait(false);
    }

    private async Task WaitForProcessExit(CancellationToken cancellation)
    {
        try
        {
            await _process.WaitForExitAsync(cancellation);
            ExitCode = _process.ExitCode;

            await _outEvent.WaitAsync(cancellation);
            await _errEvent.WaitAsync(cancellation);

            await _outputWriter.FlushAsync(cancellation);
            await _errorWriter.FlushAsync(cancellation);
            _outputWriter.BaseStream.Position = 0;
            _errorWriter.BaseStream.Position = 0;

            Output = new StreamReader(_outputWriter.BaseStream, _outputWriter.Encoding);
            Error = new StreamReader(_errorWriter.BaseStream, _errorWriter.Encoding);
        }
        catch (OperationCanceledException)
        {
            _process.Kill(true);
            var message = $"{string.Join(' ', _process.StartInfo.FileName, _process.StartInfo.Arguments)} Timed out after {_timeout}";
            await _errorWriter.WriteLineAsync(message);
            throw new TimeoutException(message);
        }
    }

    private async Task WriteAsync(StreamWriter writer, string what, ManualResetEventSlim evt, CancellationToken cancellation)
    {
        // When we receive null, no more input is expected, so we set the event
        if (what == null)
        {
            evt.Set();
            return;
        }
        await _outputLock.WaitAsync(cancellation);

        try
        {
            await writer.WriteLineAsync(what);
        }
        finally
        {
            _outputLock.Release();
        }
    }

    public void Dispose()
    {
        _outputWriter?.Dispose();
        _errorWriter?.Dispose();
        Output?.Dispose();
        Error?.Dispose();
    }
}