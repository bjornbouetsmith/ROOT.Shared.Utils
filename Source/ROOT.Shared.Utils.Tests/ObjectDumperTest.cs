using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Serialization;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class ObjectDumperTest
    {
        [TestMethod]
        public void SimpleDump()
        {
            var expected = "Hello world";
            var str = expected.Dump();
            Console.WriteLine(str);
            Assert.AreEqual(expected, str);
        }

        [TestMethod]
        public void DateTimeDump()
        {
            var dt = new DateTime(2019, 06, 06);

            var str = dt.Dump();
            Console.WriteLine(str);
        }
    }
}
