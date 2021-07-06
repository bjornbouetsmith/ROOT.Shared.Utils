using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class ProcessCallTest
    {
        [TestMethod]
        public void SimpleCall()
        {

            var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe");

            var result = call.LoadResponse("/?");

            Assert.IsTrue(result.Success);
            Console.WriteLine(result.StdOut);

        }

        [TestMethod]
        public void PipeTestGeneration()
        {
            var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe", "/?");

            call = call.Pipe(new ProcessCall("C:\\Windows\\System32\\findstr.exe", "YD"));


            Assert.AreEqual("C:\\Windows\\System32\\diskperf.exe /? | C:\\Windows\\System32\\findstr.exe YD", call.FullCommandLine);

            Console.WriteLine(call.FullCommandLine);
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
            var call = new ProcessCall("C:\\Windows\\System32\\diskperf.exe", "/?");

            call = call.Pipe(new ProcessCall("C:\\Windows\\System32\\findstr.exe", "YD"));


            var response = call.LoadResponse();
            Assert.IsTrue(response.Success);
            Console.WriteLine(response.StdOut);
            Assert.AreEqual("-YD Enables the disk performance counters for physical drives.", response.StdOut.Trim());


        }
        [TestMethod]
        public void RemoteProcessCall()
        {
            var remote = new RemoteProcessCall("bbs", "zfsdev.root.dom", true);

            var full = remote | new ProcessCall("/usr/sbin/zfs", "get all");

            Console.WriteLine(full.FullCommandLine);

        }
        [TestMethod]
        public void RemoteProcessCallAsProcessCall()
        {
            ProcessCall remote = new RemoteProcessCall("bbs", "zfsdev.root.dom", true);

            var full = remote | new ProcessCall("/usr/sbin/zfs", "get all");

            var exe = full.Execute();
            Console.WriteLine(full.FullCommandLine);
            Console.WriteLine(exe.FullCommandLine);

        }
    }
}
