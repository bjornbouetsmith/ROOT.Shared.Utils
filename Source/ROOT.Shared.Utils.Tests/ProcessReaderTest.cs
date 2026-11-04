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
public class ProcessReaderTest
{
    [TestMethod]
    public void Constructor_WithoutRedirectedStandardStreams_ThrowsArgumentException()
    {
        var startInfo = new ProcessStartInfo("process-reader-test");
        using var process = new Process { StartInfo = startInfo };

        var exception = Assert.ThrowsExactly<ArgumentException>(
            () => new ProcessReader(process, TimeSpan.FromSeconds(1)));

        Assert.AreEqual("process", exception.ParamName);
        StringAssert.Contains(exception.Message, "RedirectStandardOutput");
    }

    [TestMethod]
    public void Constructor_WithInputStreamButWithoutRedirectedStandardInput_ThrowsArgumentException()
    {
        var startInfo = new ProcessStartInfo("process-reader-test")
        {
            RedirectStandardOutput = true
        };
        using var process = new Process { StartInfo = startInfo };
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("ignored"));

        var exception = Assert.ThrowsExactly<ArgumentException>(
            () => new ProcessReader(process, TimeSpan.FromSeconds(1), input));

        Assert.AreEqual("process", exception.ParamName);
        StringAssert.Contains(exception.Message, "RedirectStandardInput");
    }

    [TestMethod]
    public async Task Dispose_AfterProcessCompletes_IsIdempotentAndReleasesOutput()
    {
        using var process = CreateShortDelayProcess();
        var reader = new ProcessReader(process, TimeSpan.FromSeconds(10));
        await reader.WaitAsync(CancellationToken.None);
        var outputStream = reader.Output.BaseStream;

        reader.Dispose();
        reader.Dispose();

        Assert.IsFalse(outputStream.CanRead);
    }

    [TestMethod]
    public async Task WaitAsync_ForRootDirectoryListing_ReturnsExitCodeAndStandardOutput()
    {
        using var process = CreateRootListingProcess(out var expectedOutput);
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(30));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await reader.WaitAsync(cancellation.Token);

        var output = reader.Output.ReadToEnd();
        Assert.AreEqual(0, reader.ExitCode);
        StringAssert.Contains(output, expectedOutput);
        StringAssert.Contains(output, Environment.NewLine);
        Assert.AreEqual(string.Empty, reader.Error.ReadToEnd().Trim());
    }


    [TestMethod]
    public async Task WaitAsync_WhenProcessExitsWithNonZeroCode_CapturesExitCodeAndError()
    {
        using var process = CreateFailingProcess();
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(10));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await reader.WaitAsync(cancellation.Token);

        Assert.AreEqual(3, reader.ExitCode);
        Assert.AreEqual("error", reader.Error.ReadToEnd().Trim());
        Assert.AreEqual(string.Empty, reader.Output.ReadToEnd().Trim());
    }

    [TestMethod]
    public async Task WaitAsync_WithDistinctInputAndOutputEncodings_DecodesWithOutputEncoding()
    {
        using var process = CreateRootListingProcess(out _);
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardInputEncoding = Encoding.ASCII;
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(30));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await reader.WaitAsync(cancellation.Token);

        Assert.AreEqual(Encoding.UTF8.WebName, reader.Output.CurrentEncoding.WebName);
        StringAssert.Contains(reader.Output.ReadToEnd(), Environment.NewLine);
    }

    [TestMethod]
    public async Task WaitAsync_WhenOnlyStandardOutputIsRedirected_CapturesOutput()
    {
        using var process = CreateSingleStreamProcess(writeToStandardError: false);
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(10));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await reader.WaitAsync(cancellation.Token);

        Assert.AreEqual(0, reader.ExitCode);
        Assert.AreEqual("stream", reader.Output.ReadToEnd().Trim());
        Assert.IsNull(reader.Error);
    }

    [TestMethod]
    public async Task WaitAsync_WhenOnlyStandardErrorIsRedirected_CapturesError()
    {
        using var process = CreateSingleStreamProcess(writeToStandardError: true);
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(10));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await reader.WaitAsync(cancellation.Token);

        Assert.AreEqual(0, reader.ExitCode);
        Assert.AreEqual("stream", reader.Error.ReadToEnd().Trim());
        Assert.IsNull(reader.Output);
    }

    [TestMethod]
    public async Task WaitAsync_WithInputStream_ForwardsInputToProcessStandardInput()
    {
        using var process = CreateStandardInputEchoProcess(out var inputText);
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(30), input);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await reader.WaitAsync(cancellation.Token);

        Assert.AreEqual(0, reader.ExitCode);
        Assert.AreEqual("hello stdin", reader.Output.ReadToEnd().Trim());
        Assert.AreEqual(string.Empty, reader.Error.ReadToEnd().Trim());
    }

    [TestMethod]
    public async Task WaitAsync_DoesNotCaptureSynchronizationContext()
    {
        var originalContext = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            using var process = CreateShortDelayProcess();
            using var reader = new ProcessReader(process, TimeSpan.FromSeconds(10));

            await reader.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(0, reader.ExitCode);
            Assert.AreEqual(0, context.PostCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [TestMethod]
    public async Task WaitAsync_WhenProcessExceedsConfiguredTimeout_ThrowsTimeoutException()
    {
        using var process = CreateThreeSecondProcess();
        using var reader = new ProcessReader(process, TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => reader.WaitAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task WaitAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        using var process = CreateLongRunningProcess();
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(30));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<OperationCanceledException>(() => reader.WaitAsync(cancellation.Token));
    }

    [TestMethod]
    public async Task WaitAsync_WhenChildReadsStandardInputToEof_ClosesStandardInput()
    {
        var inputText = "b" + Environment.NewLine + "a" + Environment.NewLine;
        using var process = CreateSortProcess();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
        using var reader = new ProcessReader(process, TimeSpan.FromSeconds(10), input);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await reader.WaitAsync(cancellation.Token);

        var expected = "a" + Environment.NewLine + "b" + Environment.NewLine;
        Assert.AreEqual(expected, reader.Output.ReadToEnd());
    }

    [TestMethod]
    public async Task WaitAsync_WithBinaryInput_PreservesBytes()
    {
        var inputBytes = new byte[] { 0x00, 0x01, 0x7F, 0x80, 0x81, 0xFE, 0xFF, 0x41, 0x0A };
        var outputPath = Path.GetTempFileName();
        try
        {
            using var process = CreateRawStdinCopierProcess(outputPath);
            using var input = new MemoryStream(inputBytes);
            using var reader = new ProcessReader(process, TimeSpan.FromSeconds(30), input);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await reader.WaitAsync(cancellation.Token);

            CollectionAssert.AreEqual(inputBytes, File.ReadAllBytes(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [TestMethod]
    public async Task WaitAsync_WhenCancelledWhileProcessWritesOutput_DoesNotRaiseUnhandledException()
    {
        var unhandled = new int[1];
        void OnUnhandled(object sender, UnhandledExceptionEventArgs e) => Interlocked.Increment(ref unhandled[0]);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
        try
        {
            using var process = CreateFloodingProcess();
            using var reader = new ProcessReader(process, TimeSpan.FromSeconds(30));
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            await Assert.ThrowsAsync<OperationCanceledException>(() => reader.WaitAsync(cancellation.Token));
            await Task.Delay(500);

            Assert.AreEqual(0, unhandled[0]);
        }
        finally
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandled;
        }
    }

    private static Process CreateSingleStreamProcess(bool writeToStandardError)
    {
        ProcessStartInfo startInfo;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cmd = GetSystemExecutable("cmd.exe");
            startInfo = new ProcessStartInfo(cmd)
            {
                RedirectStandardError = writeToStandardError,
                RedirectStandardOutput = !writeToStandardError,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("/C");
            startInfo.ArgumentList.Add(writeToStandardError ? "echo stream 1>&2" : "echo stream");
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string sh = "/bin/sh";
            if (!File.Exists(sh))
            {
                Assert.Inconclusive($"Test executable not found: {sh}");
            }

            startInfo = new ProcessStartInfo(sh)
            {
                RedirectStandardError = writeToStandardError,
                RedirectStandardOutput = !writeToStandardError,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(writeToStandardError ? "echo stream >&2" : "echo stream");
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static Process CreateShortDelayProcess()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var ping = GetSystemExecutable("ping.exe");
            var startInfo = CreateRedirectedStartInfo(ping);
            startInfo.ArgumentList.Add("-n");
            startInfo.ArgumentList.Add("2");
            startInfo.ArgumentList.Add("127.0.0.1");
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string sleep = "/bin/sleep";
            if (!File.Exists(sleep))
            {
                Assert.Inconclusive($"Test executable not found: {sleep}");
            }

            var startInfo = CreateRedirectedStartInfo(sleep);
            startInfo.ArgumentList.Add("1");
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static Process CreateThreeSecondProcess()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var ping = GetSystemExecutable("ping.exe");
            var startInfo = CreateRedirectedStartInfo(ping);
            startInfo.ArgumentList.Add("-n");
            startInfo.ArgumentList.Add("4");
            startInfo.ArgumentList.Add("127.0.0.1");
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string sleep = "/bin/sleep";
            if (!File.Exists(sleep))
            {
                Assert.Inconclusive($"Test executable not found: {sleep}");
            }

            var startInfo = CreateRedirectedStartInfo(sleep);
            startInfo.ArgumentList.Add("3");
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static Process CreateSortProcess()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var sort = GetSystemExecutable("sort.exe");
            return new Process { StartInfo = CreateRedirectedStartInfo(sort) };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var sort = File.Exists("/usr/bin/sort") ? "/usr/bin/sort" : "/bin/sort";
            if (!File.Exists(sort))
            {
                Assert.Inconclusive($"Test executable not found: {sort}");
            }

            return new Process { StartInfo = CreateRedirectedStartInfo(sort) };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static Process CreateRawStdinCopierProcess(string outputPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var powershell = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe");

            if (!File.Exists(powershell))
            {
                Assert.Inconclusive($"Test executable not found: {powershell}");
            }

            var escapedPath = outputPath.Replace("'", "''");
            var script = "$stdinStream=[Console]::OpenStandardInput(); "
                + $"$outFile=[System.IO.File]::Create('{escapedPath}'); "
                + "$stdinStream.CopyTo($outFile); $outFile.Dispose()";

            var startInfo = CreateRedirectedStartInfo(powershell);
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(script);
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string sh = "/bin/sh";
            if (!File.Exists(sh))
            {
                Assert.Inconclusive($"Test executable not found: {sh}");
            }

            var escapedPath = outputPath.Replace("'", "'\\''");
            var startInfo = CreateRedirectedStartInfo(sh);
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add($"cat > '{escapedPath}'");
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static Process CreateFloodingProcess()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cmd = GetSystemExecutable("cmd.exe");
            var startInfo = CreateRedirectedStartInfo(cmd);
            startInfo.ArgumentList.Add("/C");
            startInfo.ArgumentList.Add("for /L %i in (1,1,1000000) do @(echo out%i & echo err%i 1>&2)");
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string sh = "/bin/sh";
            if (!File.Exists(sh))
            {
                Assert.Inconclusive($"Test executable not found: {sh}");
            }

            var startInfo = CreateRedirectedStartInfo(sh);
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("i=0; while true; do echo out$i; echo err$i >&2; i=$((i+1)); done");
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static Process CreateRootListingProcess(out string expectedOutput)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cmd = GetSystemExecutable("cmd.exe");
            var startInfo = CreateRedirectedStartInfo(cmd);
            startInfo.ArgumentList.Add("/C");
            startInfo.ArgumentList.Add("dir");
            startInfo.ArgumentList.Add("C:\\");
            expectedOutput = "C:\\";
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string ls = "/bin/ls";
            if (!File.Exists(ls))
            {
                Assert.Inconclusive($"Test executable not found: {ls}");
            }

            var startInfo = CreateRedirectedStartInfo(ls);
            startInfo.ArgumentList.Add("-l");
            startInfo.ArgumentList.Add("/");
            expectedOutput = "etc";
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        expectedOutput = null;
        return null;
    }

    private static Process CreateFailingProcess()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cmd = GetSystemExecutable("cmd.exe");
            var startInfo = CreateRedirectedStartInfo(cmd);
            startInfo.ArgumentList.Add("/C");
            startInfo.ArgumentList.Add("echo error 1>&2 & exit /b 3");
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string sh = "/bin/sh";
            if (!File.Exists(sh))
            {
                Assert.Inconclusive($"Test executable not found: {sh}");
            }

            var startInfo = CreateRedirectedStartInfo(sh);
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("echo error 1>&2; exit 3");
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static Process CreateStandardInputEchoProcess(out string inputText)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cmd = GetSystemExecutable("cmd.exe");
            var startInfo = CreateRedirectedStartInfo(cmd);
            startInfo.ArgumentList.Add("/V:ON");
            startInfo.ArgumentList.Add("/C");
            startInfo.ArgumentList.Add("set /p line= & echo !line!");
            inputText = "hello stdin" + Environment.NewLine;
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string head = "/bin/head";
            if (!File.Exists(head))
            {
                Assert.Inconclusive($"Test executable not found: {head}");
            }

            var startInfo = CreateRedirectedStartInfo(head);
            startInfo.ArgumentList.Add("-n");
            startInfo.ArgumentList.Add("1");
            inputText = "hello stdin" + Environment.NewLine;
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        inputText = null;
        return null;
    }

    private static Process CreateLongRunningProcess()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var ping = GetSystemExecutable("ping.exe");
            var startInfo = CreateRedirectedStartInfo(ping);
            startInfo.ArgumentList.Add("-n");
            startInfo.ArgumentList.Add("10");
            startInfo.ArgumentList.Add("127.0.0.1");
            return new Process { StartInfo = startInfo };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            const string sleep = "/bin/sleep";
            if (!File.Exists(sleep))
            {
                Assert.Inconclusive($"Test executable not found: {sleep}");
            }

            var startInfo = CreateRedirectedStartInfo(sleep);
            startInfo.ArgumentList.Add("30");
            return new Process { StartInfo = startInfo };
        }

        Assert.Inconclusive("The test requires Windows or Linux.");
        return null;
    }

    private static string GetSystemExecutable(string fileName)
    {
        var path = Path.Combine(Environment.SystemDirectory, fileName);
        if (!File.Exists(path))
        {
            Assert.Inconclusive($"Test executable not found: {path}");
        }

        return path;
    }

    private static ProcessStartInfo CreateRedirectedStartInfo(string fileName)
    {
        return new ProcessStartInfo(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
    }

    private sealed class CountingSynchronizationContext : SynchronizationContext
    {
        private int _postCount;

        public int PostCount => Volatile.Read(ref _postCount);

        public override void Post(SendOrPostCallback d, object state)
        {
            Interlocked.Increment(ref _postCount);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }
}
