using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests;

[TestClass]
public class SshVerificationTest
{
    private const string Image = "linuxserver/openssh-server:10.3_p1-r1-ls241";
    private static readonly string ContainerName = $"root-shared-utils-ssh-verification-{Environment.ProcessId}";
    private const string TestUser = "testuser";

    [TestMethod]
    public async Task SshProcessCall_FromWindowsToLinux_RunsRemoteCommand()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var docker = FindExecutable("docker.exe");
        var ssh = FindExecutable("ssh.exe");
        var sshKeyGen = FindExecutable("ssh-keygen.exe");

        if (docker == null)
        {
            Assert.Inconclusive("docker.exe was not found on PATH.");
            return;
        }

        if (ssh == null)
        {
            Assert.Inconclusive("ssh.exe was not found on PATH.");
            return;
        }

        if (sshKeyGen == null)
        {
            Assert.Inconclusive("ssh-keygen.exe was not found on PATH.");
            return;
        }

        var workDirectory = Path.Combine(Path.GetTempPath(), "ROOT.Shared.Utils.SshVerification");
        if (Directory.Exists(workDirectory))
        {
            Directory.Delete(workDirectory, true);
        }

        Directory.CreateDirectory(workDirectory);
        workDirectory = GetShortPath(workDirectory);
        var privateKeyPath = Path.Combine(workDirectory, "id_ed25519");
        var publicKeyPath = privateKeyPath + ".pub";

        try
        {
            var version = await RunProcessAsync(
                docker,
                new[] { "version", "--format", "{{.Server.Version}}" },
                TimeSpan.FromSeconds(30));

            if (version.ExitCode != 0)
            {
                Assert.Inconclusive($"Docker is not running: {version.StdError.Trim()}");
                return;
            }

            await RunProcessAsync(
                docker,
                new[] { "rm", "-f", ContainerName },
                TimeSpan.FromSeconds(30));

            var keyGen = await RunProcessAsync(
                sshKeyGen,
                new[] { "-q", "-t", "ed25519", "-N", "", "-f", privateKeyPath },
                TimeSpan.FromSeconds(30));

            Assert.AreEqual(0, keyGen.ExitCode, keyGen.StdError);

            var publicKey = (await File.ReadAllTextAsync(publicKeyPath)).Trim();

            var run = await RunProcessAsync(
                docker,
                new[]
                {
                    "run", "-d", "--name", ContainerName,
                    "-p", "127.0.0.1::2222",
                    "-e", "PUID=1000",
                    "-e", "PGID=1000",
                    "-e", "TZ=Etc/UTC",
                    "-e", $"USER_NAME={TestUser}",
                    "-e", $"PUBLIC_KEY={publicKey}",
                    "-e", "PASSWORD_ACCESS=false",
                    "-e", "SUDO_ACCESS=false",
                    Image,
                },
                TimeSpan.FromMinutes(5));

            Assert.AreEqual(0, run.ExitCode, run.StdError);

            var port = await GetHostPortAsync(docker);
            await WaitForSshAsync(ssh, privateKeyPath, port);

            // SSHProcessCall only accepts user@host. Prefixing the username with ssh options
            // keeps the test on the real SSHProcessCall -> Pipe -> ProcessCall path while
            // pointing it at the temporary container and key.
            var keyArgument = privateKeyPath.Contains(' ') ? $"\"{privateKeyPath}\"" : privateKeyPath;
            var remote = new SSHProcessCall(
                $"-i {keyArgument} -p {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o BatchMode=yes {TestUser}",
                "127.0.0.1")
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            var call = remote | new ProcessCall("/bin/echo", "remote-ok");
            var result = await call.LoadResponseAsync(false);

            Assert.AreEqual(0, result.ExitCode, result.StdError);
            Assert.AreEqual("remote-ok", result.StdOut.Trim());
        }
        finally
        {
            await RunProcessAsync(
                docker,
                new[] { "rm", "-f", ContainerName },
                TimeSpan.FromSeconds(60));

            try
            {
                Directory.Delete(workDirectory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static async Task<string> GetHostPortAsync(string docker)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var result = await RunProcessAsync(
                docker,
                new[] { "port", ContainerName, "2222/tcp" },
                TimeSpan.FromSeconds(10));

            if (result.ExitCode == 0)
            {
                var lastLine = result.StdOut.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (lastLine != null)
                {
                    var separator = lastLine.LastIndexOf(':');
                    if (separator >= 0)
                    {
                        return lastLine[(separator + 1)..].Trim();
                    }
                }
            }

            await Task.Delay(500);
        }

        Assert.Fail("Could not determine the published SSH port for the test container.");
        return null;
    }

    private static async Task WaitForSshAsync(string ssh, string privateKeyPath, string port)
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);
        var lastError = string.Empty;

        while (DateTime.UtcNow < deadline)
        {
            var result = await RunProcessAsync(
                ssh,
                new[]
                {
                    "-i", privateKeyPath,
                    "-p", port,
                    "-o", "StrictHostKeyChecking=no",
                    "-o", "UserKnownHostsFile=NUL",
                    "-o", "BatchMode=yes",
                    "-o", "ConnectTimeout=2",
                    $"{TestUser}@127.0.0.1",
                    "echo ready",
                },
                TimeSpan.FromSeconds(15));

            if (result.ExitCode == 0 && result.StdOut.Contains("ready", StringComparison.Ordinal))
            {
                return;
            }

            lastError = result.StdError.Trim();
            await Task.Delay(1000);
        }

        Assert.Fail($"The SSH server did not become ready. Last error: {lastError}");
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            Assert.Inconclusive($"Could not start '{fileName}': {exception.Message}");
            return null;
        }

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(true);
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }

            Assert.Fail($"'{fileName}' did not finish within {timeout}.");
        }

        return new ProcessResult(process.ExitCode, await stdOutTask, await stdErrTask);
    }

    private static string FindExecutable(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var directory in path.Split(Path.PathSeparator))
        {
            if (directory.Length == 0)
            {
                continue;
            }

            var candidate = Path.Combine(directory.Trim('"'), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string GetShortPath(string path)
    {
        var buffer = new StringBuilder(1024);
        var length = GetShortPathName(path, buffer, (uint)buffer.Capacity);

        return length > 0 && length < buffer.Capacity ? buffer.ToString() : path;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathName(string longPath, StringBuilder shortPath, uint bufferLength);

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdError);
}
