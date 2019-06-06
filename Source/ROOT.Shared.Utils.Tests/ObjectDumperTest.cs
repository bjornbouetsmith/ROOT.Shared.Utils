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
            string expected = "2019-06-06 12:00:00.000";
            var str = dt.Dump();
            Console.WriteLine(str);
            Assert.AreEqual(expected, str);
        }

        [TestMethod]
        public void SimpleClass()
        {
            var outer = new Outer();
            outer.Inner = new Inner { Empty=new EmptyClass(),My = MyEnum.Value1};

            var str = outer.Dump();

            Console.WriteLine(str);

        }

        [TestMethod]
        public void EmptyClassWithoutPropertiesTest()
        {
            var empty = new EmptyClass();

            var str = empty.Dump();
            Console.WriteLine(str);
        }

        [TestMethod]
        public void EmptyClassWithoutPropertiesTestNull()
        {
            var empty = new EmptyClass();

            var str = ObjectDumper.Dump<EmptyClass>(null);
            Console.WriteLine(str);
        }
    }

    public class Outer
    {
        public Inner Inner { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }

    public class Inner
    {
        public MyEnum My { get; set; }
        public EmptyClass Empty { get; set; }
    }

    public enum MyEnum
    {
        Value0 = 0,
        Value1 = 1
    }

    public class EmptyClass
    {

    }
}
