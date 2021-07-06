using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ROOT.Shared.Utils.OS
{
    public static class SSH
    {
        const string WindowsSSh = "C:\\Windows\\System32\\OpenSSH\\ssh.exe";
        private const string UnixSsh = "/usr/bin/ssh";


        public static string BinPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsSSh : UnixSsh;
    }

    public static class Sudo
    {
        const string WindowsSudo = "/usr/bin/sudo";
        private const string UnixSudo = "/usr/bin/sudo";
        public static string BinPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsSudo : UnixSudo;
    }


    /// <summary>
    /// Remote process call.
    /// using sudo requires no password required for sudo command
    /// i.e. [username]  ALL=(ALL)       NOPASSWD: ALL
    /// in /etc/sudoers
    /// </summary>
    public class RemoteProcessCall : ProcessCall
    {
        public bool RequiresSudo { get; }

        public RemoteProcessCall(string username, string hostName, bool requiresSudo = false)
            : base(SSH.BinPath, $"{username}@{hostName}")
        {
            RequiresSudo = requiresSudo;
        }

        public static ProcessCall operator |(RemoteProcessCall first, ProcessCall second)
        {
            return first.Pipe(second);
        }
    }

    public class ProcessReader : IDisposable
    {
        private readonly Process _process;
        private readonly TimeSpan _timeout;
        private readonly Stream _inputStream;
        private StreamWriter _outputWriter;
        private StreamWriter _errorWriter;
        private object _factoryLock = new object();
        private ManualResetEventSlim _outEvent = new ManualResetEventSlim(false);
        private ManualResetEventSlim _errEvent = new ManualResetEventSlim(false);
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

        public bool Wait()
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
                _process.Kill();
                _errorWriter.WriteLine($"Timed out after {_timeout}");
            }

            ExitCode = success ? _process.ExitCode : -1;
            
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
            return success;
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

    public class ProcessCall
    {
        public string BinPath { get; }
        public string Arguments { get; }
        public string FullCommandLine { get; }
        public string Shell { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

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

        public ProcessCallResult LoadResponse(params string[] arguments)
        {
            return LoadResponse(null, arguments);
        }

        public ProcessCallResult LoadResponse(Stream inputStream, params string[] arguments)
        {
            var exec = this.Execute();
            Started = true;
            string args = exec.Arguments;
            if (arguments != null && arguments.Length > 0)
            {
                args += " " + string.Join(" ", arguments);
            }

            args = args.Trim();

            var startInfo = new ProcessStartInfo(exec.BinPath, args)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,

            };

            using var process = new Process { StartInfo = startInfo };
            using var reader = new ProcessReader(process, Timeout, inputStream);

            var success = reader.Wait();
            var exitCode = reader.ExitCode;

            var stdOut = reader.Output?.ReadToEnd();
            var stdError = reader.Error?.ReadToEnd();

            return new ProcessCallResult { ExitCode = exitCode, StdOut = stdOut, StdError = stdError, CommandLine = string.Join(" ", exec.BinPath, args) };


            //process.StartInfo = startInfo;



            //var output = new List<string>();
            //var error = new List<string>();
            //CopyStreamToProcessInput(inputStream, process);
            //process.OutputDataReceived += (s, e) => Write(output, e.Data);
            //process.ErrorDataReceived += (s, e) => Write(error, e.Data);
            //process.Start();
            //process.BeginErrorReadLine();
            //process.BeginOutputReadLine();

            //if (process.WaitForExit((int)Timeout.TotalMilliseconds))
            //{

            //    var exitCode = process.ExitCode;

            //    var stdOut = string.Join("\r\n", output);
            //    var stdError = string.Join("\r\n", error);
            //    return new ProcessCallResult { ExitCode = exitCode, StdOut = stdOut, StdError = stdError, CommandLine = string.Join(" ", exec.BinPath, args) };
            //}
            //else
            //{
            //    process.Kill();
            //    var stdOut = string.Join("\r\n", output);
            //    var stdError = string.Join("\r\n", error);
            //    return new ProcessCallResult { ExitCode = -1, StdOut = stdOut, StdError = $"Timed out after {Timeout}\r\n" + stdError, CommandLine = string.Join(" ", exec.BinPath, args) };
            //}
        }

        private void Write(List<string> content, string what)
        {
            lock (content)
            {
                content.Add(what);
            }
        }

        public bool Started { get; private set; }

        private static void CopyStreamToProcessInput(Stream inputStream, Process process)
        {
            if (inputStream != null)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = inputStream.Read(buffer, 0, 1024);
                while (bytesRead > 0)
                {
                    process.StandardInput.BaseStream.Write(buffer, 0, bytesRead);
                    bytesRead = inputStream.Read(buffer, 0, 1024);
                }
            }
        }

        public static ProcessCall operator |(ProcessCall first, ProcessCall second)
        {
            return first.Pipe(second);
        }
    }

    public static class ProcessCallExtensions
    {
        const string WindowsShell = "C:\\Windows\\System32\\cmd.exe";
        private const string UnixShell = "/bin/bash";

        private static readonly string Shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsShell : UnixShell;

        public static ProcessCall ForceShell(this ProcessCall processCall, OSPlatform platform)
        {
            var shell = platform == OSPlatform.Windows ? WindowsShell : UnixShell;
            return new ProcessCall(processCall.BinPath, processCall.Arguments) { Shell = shell };
        }

        public static ProcessCall Pipe(this ProcessCall processCall, ProcessCall other)
        {
            if (processCall is RemoteProcessCall remote)
            {
                return Pipe(remote, other);
            }

            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            string args = string.Concat(processCall.Arguments, " | ", other.BinPath, " ", other.Arguments).Trim();
            return new ProcessCall(processCall.BinPath, args);
        }

        public static ProcessCall Pipe(this RemoteProcessCall processCall, ProcessCall other)
        {
            if (processCall.Started || other.Started)
            {
                throw new InvalidOperationException("Cannot pipe two processes that has been started - use Pipe before you call LoadResponse");
            }

            string args = string.Concat(processCall.Arguments, " \"", processCall.RequiresSudo ? Sudo.BinPath : "", " ", other.BinPath, " ", other.Arguments, "\"").Trim();

            return new ProcessCall(processCall.BinPath, args);
        }

        public static ProcessCall Execute(this ProcessCall processCall)
        {
            if (processCall.Started)
            {
                throw new InvalidOperationException("Cannot execute  process that has been started");
            }

            string args = string.Concat("/Q /C", " ", processCall.BinPath, " ", processCall.Arguments).Trim();

            var shell = processCall.Shell ?? Shell;
            return new ProcessCall(shell, args);
        }

        public static ProcessCallException ToException(this ProcessCallResult result)
        {
            return new ProcessCallException(result.CommandLine, result.ExitCode, result.StdOut, result.StdError);
        }
    }

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

    public class ProcessCallResult
    {
        public bool Success => ExitCode == 0;
        public int ExitCode { get; set; }
        public string StdOut { get; set; }
        public string StdError { get; set; }
        public string CommandLine { get; set; }
    }
}