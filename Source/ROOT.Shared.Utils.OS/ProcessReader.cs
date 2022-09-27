using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ROOT.Shared.Utils.OS
{
    public class ProcessReader : IDisposable
    {
        private readonly Process _process;
        private readonly ProcessStartInfo _startInfo;
        private readonly TimeSpan _timeout;
        private readonly Stream _inputStream;
        private readonly StreamWriter _outputWriter;
        private readonly StreamWriter _errorWriter;
        private readonly ManualResetEventSlim _outEvent = new(false);
        private readonly ManualResetEventSlim _errEvent = new(false);

        public ProcessReader(Process process, ProcessStartInfo startInfo, TimeSpan timeout, Stream inputStream = null)
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
            _startInfo = startInfo;
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
            if (inputStream != null)
            {
                using var streamReader = new StreamReader(inputStream);

                process.StandardInput.Write(streamReader.ReadToEnd());

                //byte[] buffer = new byte[1024];
                //int bytesRead = inputStream.Read(buffer, 0, 1024);
                //while (bytesRead > 0)
                //{
                //    process.StandardInput.BaseStream.Write(buffer, 0, bytesRead);
                //    bytesRead = inputStream.Read(buffer, 0, 1024);
                //}
            }
        }

        public void Wait()
        {
            CopyStreamToProcessInput(_inputStream, _process);
            _process.OutputDataReceived += (s, e) => Write(_outputWriter, e.Data, _outEvent);
            _process.ErrorDataReceived += (s, e) => Write(_errorWriter, e.Data, _errEvent);
            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();

            bool success = _process.WaitForExit((int)_timeout.TotalMilliseconds);
            if (!success)
            {
                _process.Kill(true);
                var message = $"{string.Join(' ', _startInfo.FileName, _startInfo.Arguments)} Timed out after {_timeout}";
                _errorWriter.WriteLine(message);
                throw new TimeoutException(message);

            }

            ExitCode = _process.ExitCode;

            do
            {
            } while (!_outEvent.Wait(_timeout));

            do
            {
            } while (!_errEvent.Wait(_timeout));

            _outputWriter.Flush();
            _errorWriter.Flush();
            _outputWriter.BaseStream.Position = 0;
            _errorWriter.BaseStream.Position = 0;

            Output = new StreamReader(_outputWriter.BaseStream, _outputWriter.Encoding);
            Error = new StreamReader(_errorWriter.BaseStream, _errorWriter.Encoding);
        }

        public int ExitCode { get; private set; }

        public StreamReader Output { get; private set; }
        public StreamReader Error { get; private set; }
        private void Write(StreamWriter writer, string what, ManualResetEventSlim evt)
        {
            if (what == null)
            {
                evt.Set();
                return;
            }

            lock (writer)
            {
                writer.WriteLine(what);
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
}