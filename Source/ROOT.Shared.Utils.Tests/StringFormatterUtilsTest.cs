using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Serialization;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class StringFormatterUtilsTest
    {
        [TestMethod]
        public void TimeSpan1Test()
        {
            var ts = TimeSpan.FromSeconds(123.6);
            Console.WriteLine(ts.AsString());
        }
        [TestMethod]
        public void TimeSpan2Test()
        {
            var ts = TimeSpan.FromSeconds(23.6);
            Console.WriteLine(ts.AsString());
        }

        [TestMethod]
        public void TimeSpan3Test()
        {
            var ts = TimeSpan.FromSeconds(0.631);
            Console.WriteLine(ts.AsString());
        }

        [TestMethod]
        public void TimeSpan4Test()
        {
            var ts = TimeSpan.FromHours(6.5);
            Console.WriteLine(ts.AsString());
        }

        [TestMethod]
        public void TimeSpan5Test()
        {
            var ts = TimeSpan.FromTicks(9997);
            Console.WriteLine(ts.AsString());
        }
    }
}
