using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    }
}
