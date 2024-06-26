﻿using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Serialization;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class DateUtilsTest
    {
        [TestMethod]
        public void ToIsoDateTimeFormat()
        {
            DateTime dt = new DateTime(2019, 10, 11, 07, 08, 09, 10, DateTimeKind.Utc);

            Console.WriteLine(dt.ToIso8601DateTimeString());
            Assert.AreEqual("2019-10-11T07:08:09.010Z", dt.ToIso8601DateTimeString());
        }


        [TestMethod]
        public void ToIsoDateTimeFormat2()
        {
            DateTime dt = new DateTime(2019, 10, 11, 14, 08, 09, 10, DateTimeKind.Utc);

            Console.WriteLine(dt.ToIso8601DateTimeString());
            Assert.AreEqual("2019-10-11T14:08:09.010Z", dt.ToIso8601DateTimeString());
        }

        [TestMethod]
        public void ToIsoDateTimeFormat3()
        {

            DateTime dt = new DateTime(2019, 12, 11, 14, 08, 09, 10, DateTimeKind.Local);
            var offset = TimeZoneInfo.Local.GetUtcOffset(dt);
            Console.WriteLine(dt.ToIso8601DateTimeString());
            Assert.AreEqual($"2019-12-11T14:08:09.010{ (offset >= TimeSpan.Zero ? "+":"-")+ offset.ToString(@"hh\:mm")}", dt.ToIso8601DateTimeString());
        }

        [TestMethod]
        public void ToIsoDateFormat()
        {
            DateTime dt = new DateTime(2019, 12, 11, 07, 08, 09, 10, DateTimeKind.Utc);

            Console.WriteLine(dt.ToIso8601DateString());
            Assert.AreEqual("2019-12-11", dt.ToIso8601DateString());
        }
    }
}
