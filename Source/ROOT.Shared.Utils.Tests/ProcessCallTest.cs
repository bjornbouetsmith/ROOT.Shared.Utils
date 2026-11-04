using System;
using System.IO;
using System.Runtime.InteropServices;
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
            ? ("C:\\Windows\\System32\\cmd.exe", new[] { "/C", "echo", "async hello" })
            : ("/bin/echo", new[] { "async hello" });

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
    public async Task LoadResponseAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var call = new ProcessCall("/bin/echo", "hello") { UseShell = false };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => call.LoadResponseAsync(false, cancellationToken: cancellation.Token));
    }
}