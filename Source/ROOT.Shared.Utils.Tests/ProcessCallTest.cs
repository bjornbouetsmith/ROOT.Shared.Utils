using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests;

[TestClass]
public class ProcessCallTest
{
    [TestMethod]
    public async Task SimpleCallWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe");
        Console.WriteLine(call.Execute().FullCommandLine);

        var result = await call.LoadResponseAsync(false, new ProcessCallArguments("/?"));

        Assert.IsTrue(result.Success);
        Console.WriteLine(result.StdOut);

    }

    [TestMethod]
    public async Task PipeTestGenerationWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe", "/?");

        call |= new ProcessCall("C:\\Windows\\System32\\findstr.exe", "YD");


        Assert.AreEqual("C:\\Windows\\System32\\diskperf.exe /? | C:\\Windows\\System32\\findstr.exe YD", call.FullCommandLine);

        Console.WriteLine(call.FullCommandLine);
        Console.WriteLine(call.Execute().FullCommandLine);
        var result = await call.LoadResponseAsync(false);

        Assert.IsTrue(result.Success);
        Console.WriteLine(result.StdOut);
    }

    [TestMethod]
    public void PipeTestGeneration2()
    {
        var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe", "/?");

        call |= new ProcessCall("C:\\Windows\\System32\\findstr.exe", "YD");


        Assert.AreEqual("C:\\Windows\\System32\\diskperf.exe /? | C:\\Windows\\System32\\findstr.exe YD", call.FullCommandLine);

        Console.WriteLine(call.FullCommandLine);
    }

    [TestMethod]
    public async Task PipeExecution()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe", "/?");

        call |= new ProcessCall("C:\\Windows\\System32\\findstr.exe", "YD");


        var response = await call.LoadResponseAsync(false);
        Assert.IsTrue(response.Success);
        Console.WriteLine(response.StdOut);
        Assert.AreEqual("-YD Enables the disk performance counters for physical drives.", response.StdOut.Trim());


    }
    [TestMethod]
    public void RemoteProcessCall()
    {
        var remote = new SSHProcessCall("bbs", "zfsdev.root.dom", true);

        var full = remote | new ProcessCall("/usr/sbin/zfs", "get all");

        Console.WriteLine(full.FullCommandLine);

    }
    [TestMethod]
    public void RemoteProcessCallAsProcessCall()
    {
        ProcessCall remote = new SSHProcessCall("bbs", "zfsdev.root.dom", true);

        var full = remote | new ProcessCall("/usr/sbin/zfs", "get all");

        var exe = full.Execute();
        Console.WriteLine(full.FullCommandLine);
        Console.WriteLine(exe.FullCommandLine);

    }

    [TestMethod]
    [DataRow(true, ProcessCallExtensions.WindowsShell, ProcessCallExtensions.WindowsShellPrefix)]
    [DataRow(false, ProcessCallExtensions.WindowsShell, ProcessCallExtensions.WindowsShellPrefix)]
    [DataRow(true, ProcessCallExtensions.UnixShell, ProcessCallExtensions.UnixShellPrefix)]
    [DataRow(false, ProcessCallExtensions.UnixShell, ProcessCallExtensions.UnixShellPrefix)]
    public void ProcessCallExecuteTest(bool useShell, string shell, string shellPrefix)
    {
        ProcessCallExtensions.ShellPrefix = shellPrefix;
        var pc = new ProcessCall("/usr/sbin/zfs", "list") | new ProcessCall("/usr/bin/grep", "MOUNT");
        pc.UseShell = useShell;
        pc.Shell = shell;
        var cmd = pc.Execute();
        Console.WriteLine(cmd.FullCommandLine);
        var expected = useShell ? $"{shell} {shellPrefix} \"/usr/sbin/zfs list | /usr/bin/grep MOUNT\"" : "/usr/sbin/zfs list | /usr/bin/grep MOUNT";
        Assert.AreEqual(expected, cmd.FullCommandLine);
    }

    [TestMethod]
    public void PipeLeftNullsTest()
    {
        ProcessCall pc = null;

        pc |= new ProcessCall("/sbin/zfs", "list");

        Console.WriteLine(pc.FullCommandLine);
        Assert.AreEqual("/sbin/zfs list", pc.FullCommandLine);
    }

    [TestMethod]
    public void PipeLeftLocalMachineProcessCallTest()
    {
        ProcessCall pc = new LocalMachineProcessCall();

        pc |= new ProcessCall("/sbin/zfs", "list");

        Console.WriteLine(pc.FullCommandLine);
        Assert.AreEqual("/sbin/zfs list", pc.FullCommandLine);
    }

    [TestMethod]
    public void PipeRightNullsTest()
    {
        ProcessCall pc = new ProcessCall("/sbin/zfs", "list");

        pc |= null;

        Console.WriteLine(pc.FullCommandLine);
        Assert.AreEqual("/sbin/zfs list", pc.FullCommandLine);
    }

    [TestMethod]
    public void PipeRighLocalMachineProcessCallTest()
    {
        IProcessCall pc = new ProcessCall("/sbin/zfs", "list");

        pc |= new LocalMachineProcessCall();

        Console.WriteLine(pc.FullCommandLine);
        Assert.AreEqual("/sbin/zfs list", pc.FullCommandLine);
    }

    [TestMethod]
    public async Task MockProcessCallTest()
    {
        var mock = new MockProcessCall();
        mock.StdOutput = "Hello world";
        IProcessCall pc = mock;

        pc |= new ProcessCall("/sbin/zfs", "list");
        var result = await pc.LoadResponseAsync(false);
        Assert.AreEqual(mock.StdOutput, result.StdOut);
    }

    [TestMethod]
    public void ProcessCallExtensionPipeWithNullTest()
    {
        IProcessCall pc = null;
        var result = ProcessCallExtensions.Pipe(pc, new ProcessCall("/sbin/zfs", "list"));
        Assert.AreEqual("/sbin/zfs list", result.FullCommandLine);

        result = ProcessCallExtensions.Pipe(new ProcessCall("/sbin/zfs", "list"), null);
        Assert.AreEqual("/sbin/zfs list", result.FullCommandLine);
        result = ProcessCallExtensions.Pipe(new ProcessCall("/sbin/zfs", "list"), (IProcessCall)null);
        Assert.AreEqual("/sbin/zfs list", result.FullCommandLine);
    }

    [TestMethod]
    public void Pipe_ForProcessCalls_PreservesConfiguration()
    {
        var first = new ProcessCall("/bin/first", "one")
        {
            Timeout = TimeSpan.FromSeconds(42),
            Shell = "/bin/zsh",
            UseShell = false,
            RequiresSudo = true
        };
        var second = new ProcessCall("/bin/second", "two");

        var piped = first | second;

        Assert.AreEqual("/bin/first one | /bin/second two", piped.FullCommandLine);
        Assert.AreEqual(TimeSpan.FromSeconds(42), piped.Timeout);
        Assert.AreEqual("/bin/zsh", piped.Shell);
        Assert.IsTrue(piped.UseShell);
        Assert.IsTrue(piped.RequiresSudo);
    }

    [TestMethod]
    public void Pipe_WhenSecondProcessRequiresSudo_PreservesSudo()
    {
        var piped = new ProcessCall("/bin/first", "one")
            | new ProcessCall("/bin/second", "two") { RequiresSudo = true };

        Assert.IsTrue(piped.RequiresSudo);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ForceShell_ForProcessCall_PreservesConfigurationAndEnablesShell(bool windows)
    {
        var platform = windows ? OSPlatform.Windows : OSPlatform.Linux;
        var expectedShell = windows ? ProcessCallExtensions.WindowsShell : ProcessCallExtensions.UnixShell;
        var call = new ProcessCall("/bin/tool", "arg")
        {
            Timeout = TimeSpan.FromSeconds(42),
            UseShell = false,
            RequiresSudo = true
        };

        var forced = call.ForceShell(platform);

        Assert.AreEqual(expectedShell, forced.Shell);
        Assert.IsTrue(forced.UseShell);
        Assert.AreEqual(TimeSpan.FromSeconds(42), forced.Timeout);
        Assert.IsTrue(forced.RequiresSudo);
        Assert.AreEqual("/bin/tool arg", forced.FullCommandLine);
    }

    [TestMethod]
    public void Pipe_ForSshProcessCall_PreservesTimeoutAndDoesNotRequireLocalSudo()
    {
        var remote = new SSHProcessCall("bbs", "zfsdev.root.dom", true)
        {
            Timeout = TimeSpan.FromSeconds(42)
        };

        var full = remote | new ProcessCall("/usr/sbin/zfs", "get all");

        Assert.AreEqual(TimeSpan.FromSeconds(42), full.Timeout);
        Assert.IsFalse(full.RequiresSudo);
        StringAssert.Contains(full.Arguments, "/usr/sbin/zfs get all");
        StringAssert.Contains(full.Arguments, Sudo.BinPath);
    }

    [TestMethod]
    public void ProcessCallWithSudo()
    {

        var command = new ProcessCall("/sbin/zfs", "list");
        command.RequiresSudo = true;
        command.UseShell = false;

        Assert.AreEqual("/usr/bin/sudo /sbin/zfs list", command.Execute().FullCommandLine);
    }

    [TestMethod]
    public void LoadResponse_ForMockProcessCall_ReturnsConfiguredOutput()
    {
        var mock = new MockProcessCall
        {
            StdOutput = "sync output",
            StdError = "sync error",
            Success = true
        };
        IProcessCall pc = mock;

        var result = pc.LoadResponse(false);

        Assert.AreEqual(0, result.ExitCode);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("sync output", result.StdOut);
        Assert.AreEqual("sync error", result.StdError);
        Assert.AreEqual(mock.FullCommandLine, result.CommandLine);
    }

    [TestMethod]
    public void LoadResponse_WithInputStream_ForMockProcessCall_ReturnsConfiguredOutput()
    {
        var mock = new MockProcessCall
        {
            StdOutput = "sync input output",
            Success = true
        };
        IProcessCall pc = mock;
        using var input = new MemoryStream([1, 2, 3]);

        var result = pc.LoadResponse(false, input);

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("sync input output", result.StdOut);
    }

    [TestMethod]
    public async Task LoadResponseAsync_ForMockProcessCall_ReturnsConfiguredOutput()
    {
        var mock = new MockProcessCall
        {
            StdOutput = "async output",
            StdError = "async error",
            Success = true
        };
        IProcessCall pc = mock;

        var result = await pc.LoadResponseAsync(false);

        Assert.AreEqual(0, result.ExitCode);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("async output", result.StdOut);
        Assert.AreEqual("async error", result.StdError);
        Assert.AreEqual(mock.FullCommandLine, result.CommandLine);
    }

    [TestMethod]
    public async Task LoadResponseAsync_WithInputStream_ForMockProcessCall_ReturnsConfiguredOutput()
    {
        var mock = new MockProcessCall
        {
            StdOutput = "async input output",
            Success = true
        };
        IProcessCall pc = mock;
        using var input = new MemoryStream([1, 2, 3]);

        var result = await pc.LoadResponseAsync(false, input);

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("async input output", result.StdOut);
    }

    [TestMethod]
    public async Task LoadResponseAsync_ForFailedMockProcessCall_ThrowsWhenRequested()
    {
        var mock = new MockProcessCall
        {
            StdError = "async failure",
            Success = false
        };
        IProcessCall pc = mock;

        var exception = await Assert.ThrowsExactlyAsync<ProcessCallException>(() => pc.LoadResponseAsync(true));

        Assert.AreEqual("async failure", exception.StdError);
    }

    [TestMethod]
    public async Task LoadResponseAsync_ForLocalProcess_ReturnsOutput()
    {
        var (binPath, arguments) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ("C:\\Windows\\System32\\cmd.exe", new[] { "/C", "echo", "async", "hello" })
            : ("/bin/echo", new[] { "async", "hello" });

        if (!File.Exists(binPath))
        {
            Assert.Inconclusive($"Test executable not found: {binPath}");
        }

        var call = new ProcessCall(binPath) { UseShell = false, Timeout = TimeSpan.FromSeconds(30) };

        var result = await call.LoadResponseAsync(false, new ProcessCallArguments(arguments));

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("async hello", result.StdOut?.Trim());
        Assert.AreEqual(string.Empty, result.StdError?.Trim());
    }

    [TestMethod]
    public void ProcessCallArguments_WithArguments_ExposesArguments()
    {
        var arguments = new ProcessCallArguments("--first", "second value");

        CollectionAssert.AreEqual(new[] { "--first", "second value" }, arguments.Arguments);
    }

    [TestMethod]
    public void ProcessCallArguments_WithNullArguments_ExposesEmptyArray()
    {
        var arguments = new ProcessCallArguments(null);

        Assert.IsNotNull(arguments.Arguments);
        Assert.AreEqual(0, arguments.Arguments.Length);
    }

    [TestMethod]
    public void ProcessCallArguments_Default_ExposesEmptyArray()
    {
        ProcessCallArguments arguments = default;

        Assert.IsNotNull(arguments.Arguments);
        Assert.AreEqual(0, arguments.Arguments.Length);
    }

    [TestMethod]
    public async Task LoadResponseAsync_WithArgumentsContainingSpaces_PreservesArgumentBoundaries()
    {
        var scriptPath = CreateArgumentEchoScript();
        try
        {
            var (binPath, arguments) = CreateArgumentEchoCall(scriptPath);
            var call = new ProcessCall(binPath, arguments)
            {
                UseShell = false,
                Timeout = TimeSpan.FromSeconds(10)
            };

            var result = await call.LoadResponseAsync(
                false,
                new ProcessCallArguments("first", "second value"));

            var expected = "[first]" + Environment.NewLine + "[second value]" + Environment.NewLine;
            Assert.AreEqual(expected, result.StdOut);
        }
        finally
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }
    }

    private static string CreateArgumentEchoScript()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var path = Path.Combine(Path.GetTempPath(), "ROOT.Shared.Utils.ArgEcho.cmd");
            var script = "@echo off\r\n"
                + ":loop\r\n"
                + "if \"%~1\"==\"\" goto end\r\n"
                + "echo [%~1]\r\n"
                + "shift\r\n"
                + "goto loop\r\n"
                + ":end\r\n";
            File.WriteAllText(path, script);
            return path;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var path = Path.Combine(Path.GetTempPath(), "ROOT.Shared.Utils.ArgEcho.sh");
            var script = "#!/bin/sh\n"
                + "for argument in \"$@\"; do\n"
                + "  printf '[%s]\\n' \"$argument\"\n"
                + "done\n";
            File.WriteAllText(path, script);
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            return path;
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static (string BinPath, string Arguments) CreateArgumentEchoCall(string scriptPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ("C:\\Windows\\System32\\cmd.exe", $"/C call \"{scriptPath}\"");
        }

        return ("/bin/sh", scriptPath);
    }

    [TestMethod]
    public async Task LoadResponseAsync_WhenProcessCannotStart_DoesNotMarkAsStarted()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "ROOT.Shared.Utils.Tests", "MissingProcess", "missing.exe");
        var call = new ProcessCall(missingPath) { UseShell = false, Timeout = TimeSpan.FromSeconds(5) };

        await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(
            () => call.LoadResponseAsync(false));

        Assert.IsFalse(call.Started);
    }

    [TestMethod]
    public async Task LoadResponseAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var call = new ProcessCall("/bin/echo", "hello") { UseShell = false };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => call.LoadResponseAsync(false, cancellationToken: cancellation.Token));
    }

    [TestMethod]
    public async Task LoadResponseAsync_WhenTokenCancelledWhileRunning_ThrowsOperationCanceledException()
    {
        var (binPath, arguments) = GetLongRunningCommand();
        if (!File.Exists(binPath))
        {
            Assert.Inconclusive($"Test executable not found: {binPath}");
        }

        var call = new ProcessCall(binPath) { UseShell = false, Timeout = TimeSpan.FromSeconds(60) };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => call.LoadResponseAsync(false, new ProcessCallArguments(arguments), cancellation.Token));
    }

    [TestMethod]
    public async Task LoadResponseAsync_WhenTimeoutElapses_ThrowsTimeoutException()
    {
        var (binPath, arguments) = GetLongRunningCommand();
        if (!File.Exists(binPath))
        {
            Assert.Inconclusive($"Test executable not found: {binPath}");
        }

        var call = new ProcessCall(binPath) { UseShell = false, Timeout = TimeSpan.FromMilliseconds(200) };

        await Assert.ThrowsExactlyAsync<TimeoutException>(
            () => call.LoadResponseAsync(false, new ProcessCallArguments(arguments)));
    }

    [TestMethod]
    public async Task LoadResponseAsync_WhenCommandSucceeds_LogsSuccess()
    {
        var (binPath, arguments) = CreateExitCommand(0);
        var call = new ProcessCall(binPath, arguments) { UseShell = false, Timeout = TimeSpan.FromSeconds(10) };
        var listener = new CollectingTraceListener();
        Trace.Listeners.Add(listener);
        try
        {
            var result = await call.LoadResponseAsync(false);

            Assert.AreEqual(0, result.ExitCode);
            StringAssert.Contains(listener.Text, "success");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }

    [TestMethod]
    public async Task LoadResponseAsync_WhenCommandFails_DoesNotLogSuccess()
    {
        var (binPath, arguments) = CreateExitCommand(1);
        var call = new ProcessCall(binPath, arguments) { UseShell = false, Timeout = TimeSpan.FromSeconds(10) };
        var listener = new CollectingTraceListener();
        Trace.Listeners.Add(listener);
        try
        {
            var result = await call.LoadResponseAsync(false);

            Assert.AreEqual(1, result.ExitCode);
            Assert.IsFalse(listener.Text.Contains("success", StringComparison.Ordinal));
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }

    private static (string BinPath, string Arguments) CreateExitCommand(int exitCode)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ("C:\\Windows\\System32\\cmd.exe", $"/C exit {exitCode}");
        }

        return ("/bin/sh", $"-c \"exit {exitCode}\"");
    }

    private static (string BinPath, string[] Arguments) GetLongRunningCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ("C:\\Windows\\System32\\ping.exe", new[] { "-n", "10", "127.0.0.1" });
        }

        return ("/bin/sleep", new[] { "30" });
    }

    private sealed class CollectingTraceListener : TraceListener
    {
        private readonly StringBuilder _text = new();

        public string Text
        {
            get
            {
                lock (_text)
                {
                    return _text.ToString();
                }
            }
        }

        public override void Write(string message)
        {
            lock (_text)
            {
                _text.Append(message);
            }
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }
}