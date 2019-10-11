﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Date;

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
        public void ToIsoDateFormat()
        {
            DateTime dt = new DateTime(2019, 10, 11, 07, 08, 09, 10, DateTimeKind.Utc);

            Console.WriteLine(dt.ToIso8601DateString());
            Assert.AreEqual("2019-10-11", dt.ToIso8601DateString());
        }
    }
}