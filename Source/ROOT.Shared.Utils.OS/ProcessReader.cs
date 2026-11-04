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
    private readonly object _outputLock = new();
    private readonly object _errorLock = new();

    public int ExitCode { get; private set; }
    public StreamReader Output { get; private set; }
    public StreamReader Error { get; private set; }
    public bool Started { get; private set; }

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
            _outputWriter = new StreamWriter(new MemoryStream(), process.StartInfo.StandardOutputEncoding ?? Encoding.Unicode);
        }
    }

    private static async Task CopyStreamToProcessInputAsync(Stream inputStream, Process process, CancellationToken cancellationToken)
    {
        if (!process.StartInfo.RedirectStandardInput)
        {
            return;
        }

        try
        {
            if (inputStream != null)
            {
                await inputStream.CopyToAsync(process.StandardInput.BaseStream, cancellationToken).ConfigureAwait(false);
                await process.StandardInput.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (IOException) when (process.HasExited)
        {
            // The process exited before it consumed all input.
        }
        finally
        {
            try
            {
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // The process already closed its input stream.
            }
        }
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (_process.StartInfo.RedirectStandardOutput)
        {
            _process.OutputDataReceived += OnOutputDataReceived;
        }

        if (_process.StartInfo.RedirectStandardError)
        {
            _process.ErrorDataReceived += OnErrorDataReceived;
        }

        _process.Start();
        Started = true;

        // Start reading output before writing input, so a process that produces output
        // while consuming its input cannot fill the output pipe and deadlock.
        if (_process.StartInfo.RedirectStandardError)
        {
            _process.BeginErrorReadLine();
        }

        if (_process.StartInfo.RedirectStandardOutput)
        {
            _process.BeginOutputReadLine();
        }

        using var timeoutSource = new CancellationTokenSource(_timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            await CopyStreamToProcessInputAsync(_inputStream, _process, linkedSource.Token).ConfigureAwait(false);

            // Wait for process to exit
            await WaitForProcessExit(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            KillProcess();

            if (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
            {
                throw new TimeoutException($"{string.Join(' ', _process.StartInfo.FileName, _process.StartInfo.Arguments)} Timed out after {_timeout}");
            }

            throw;
        }
    }

    private async Task WaitForProcessExit(CancellationToken cancellation)
    {
        await _process.WaitForExitAsync(cancellation).ConfigureAwait(false);
        ExitCode = _process.ExitCode;

        if (_outputWriter != null)
        {
            await _outEvent.WaitAsync(cancellation).ConfigureAwait(false);
            await _outputWriter.FlushAsync(cancellation).ConfigureAwait(false);
            _outputWriter.BaseStream.Position = 0;
            Output = new StreamReader(_outputWriter.BaseStream, _outputWriter.Encoding);
        }

        if (_errorWriter != null)
        {
            await _errEvent.WaitAsync(cancellation).ConfigureAwait(false);
            await _errorWriter.FlushAsync(cancellation).ConfigureAwait(false);
            _errorWriter.BaseStream.Position = 0;
            Error = new StreamReader(_errorWriter.BaseStream, _errorWriter.Encoding);
        }
    }

    private void KillProcess()
    {
        try
        {
            if (_process.HasExited)
            {
                return;
            }

            _process.Kill(true);
            _process.WaitForExit(5000);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the HasExited check and the kill.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The process could not be killed; the caller still gets the cancellation.
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            _outEvent.Set();
            return;
        }

        if (_outputWriter == null)
        {
            return;
        }

        // The process raises output events on its own reader threads. Writing synchronously
        // keeps the end-of-stream event ordered after all data written for that stream.
        lock (_outputLock)
        {
            _outputWriter.WriteLine(e.Data);
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            _errEvent.Set();
            return;
        }

        if (_errorWriter == null)
        {
            return;
        }

        // Each stream has its own writer and reader thread, so a dedicated lock per stream
        // is enough and stdout/stderr do not block each other.
        lock (_errorLock)
        {
            _errorWriter.WriteLine(e.Data);
        }
    }

    public void Dispose()
    {
        _process.OutputDataReceived -= OnOutputDataReceived;
        _process.ErrorDataReceived -= OnErrorDataReceived;
        _outputWriter?.Dispose();
        _errorWriter?.Dispose();
        Output?.Dispose();
        Error?.Dispose();
        _outEvent.Dispose();
        _errEvent.Dispose();
    }
}