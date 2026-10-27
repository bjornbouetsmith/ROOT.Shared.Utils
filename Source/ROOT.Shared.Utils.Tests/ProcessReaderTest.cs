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
}
