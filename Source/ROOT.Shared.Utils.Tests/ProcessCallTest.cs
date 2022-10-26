using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class ProcessCallTest
    {
        [TestMethod]
        public void SimpleCallWindows()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe");
            Console.WriteLine(call.Execute().FullCommandLine);

            var result = call.LoadResponse(false, "/?");

            Assert.IsTrue(result.Success);
            Console.WriteLine(result.StdOut);

        }

        [TestMethod]
        public void PipeTestGenerationWindows()
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
            var result = call.LoadResponse(false);

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
        public void PipeExecution()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe", "/?");

            call |= new ProcessCall("C:\\Windows\\System32\\findstr.exe", "YD");


            var response = call.LoadResponse(false);
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
        public void PipeRightNullsTest()
        {
            ProcessCall pc = new ProcessCall("/sbin/zfs", "list");

            pc |= null;

            Console.WriteLine(pc.FullCommandLine);
            Assert.AreEqual("/sbin/zfs list", pc.FullCommandLine);
        }

        [TestMethod]
        public void MockProcessCallTest()
        {
            var mock = new MockProcessCall();
            mock.StdOutput = "Hello world";
            IProcessCall pc = mock;

            pc |= new ProcessCall("/sbin/zfs", "list");
            var result = pc.LoadResponse(false);
            Assert.AreEqual(mock.StdOutput,result.StdOut);
        }

        [TestMethod]
        public void ProcessCallWithSudo()
        {
            
            var command = new ProcessCall("/sbin/zfs", "list");
            command.RequiresSudo = true;
            command.UseShell = false;
     
            Assert.AreEqual("/usr/bin/sudo /sbin/zfs list", command.Execute().FullCommandLine);
        }
    }
}
