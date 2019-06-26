using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Date;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class EpochTest
    {
        [TestMethod]
        public void EpochToString()
        {
            Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), DateUtils.Epoch);
        }

        [TestMethod]
        public void Parse()
        {
            long seconds = 1561557827;
            var expected = new DateTime(2019, 6, 26, 14, 3, 47, 0, DateTimeKind.Utc);
            var dateTime = DateUtils.ToDateTime(seconds);

            Assert.AreEqual(expected, dateTime);
        }
    }
}
