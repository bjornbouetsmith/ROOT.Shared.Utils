using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Serialization;
using ROOT.Shared.Utils.Tests.Ebay.FindingService;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class ObjectDumperTest
    {
        [TestMethod]
        public void ClassWithArray()
        {
            var data = new WithArrayOfStrings("first", "second");

            var content = data.Dump(new JsonFormatter());

            Console.WriteLine(content);
        }

        [TestMethod]
        public void EnumerableIntDump()
        {
            IEnumerable<int> vals = new[] {1, 2, 3};

            
            var content = vals.Dump(new JsonFormatter());
            Console.WriteLine(content);
        }
        [TestMethod]
        public void EnumerableClassDump()
        {
            IEnumerable<WithPublicFields> vals = new[] { new WithPublicFields(1, 2),new WithPublicFields(3,4) };

            var str = vals.Dump(new JsonFormatter());
            Console.WriteLine(str);
            
        }

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
            var dt = new DateTime(2019, 06, 06, 0, 0, 0);
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            string expected = "2019-06-06T00:00:00.000Z";
            var str = dt.Dump();
            Console.WriteLine(str);
            Assert.AreEqual(expected, str);
        }

        [TestMethod]
        public void SimpleClass()
        {
            var outer = new Outer();
            outer.Inner = new Inner { Empty = new EmptyClass(), My = MyEnum.Value1 };

            var str = outer.Dump();

            Console.WriteLine(str);

        }

        [TestMethod]
        public void SimpleClassWithFormatter()
        {
            var outer = new Outer();
            outer.Name = "bjørn";
            outer.Price = 1.2345d;
            outer.Id = Guid.NewGuid();
            outer.Inner = new Inner { Empty = new EmptyClass(), My = MyEnum.Value1 };

            var str = outer.Dump(new JsonFormatter());

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

        [TestMethod]
        public void EbayResponseTest()
        {
            var resp = new FindItemsByKeywordsResponse();

            var str = resp.Dump();
            Console.WriteLine(str);
        }

        [TestMethod]
        public void ArrayTypes()
        {
            var withArr = new ClassWithArrayAndIEnumerable();
            withArr.Values = new[] { "First", "Secpmd" };
            withArr.IntValues = new List<int> { 1, 2, 3 };

            Console.WriteLine(withArr.Dump(new SimpleFormatter()));
        }

        [TestMethod]
        public void WithNullableTest()
        {
            var empty = new WithNullable();
            empty.GuidVal = Guid.Empty;
            var str = empty.Dump();
            Console.WriteLine(str);

            Console.WriteLine(empty.Dump(new JsonFormatter()));
        }

        [TestMethod]
        public void WithPublicFields()
        {
            var obj = new WithPublicFields(46, 42);
            obj.UNumber = 123;

            Console.WriteLine(obj.Dump());

            Console.WriteLine(obj.Dump(new JsonFormatter()));
        }

        [TestMethod]
        public void DictionaryTest()
        {
            var withDic = new WithDictionary();
            withDic.Data = new Dictionary<string, WithNullable>();
            withDic.Data["test"] = new WithNullable { GuidVal = Guid.Empty };

            Console.WriteLine(withDic.Dump());

            Console.WriteLine(withDic.Dump(new JsonFormatter()));
        }
    }


    public class WithDictionary
    {
        public Dictionary<string, WithNullable> Data { get; set; }
    }

    public class WithArrayOfStrings
    {
        public WithArrayOfStrings(params string[] validValues)
        {
            ValidValues = validValues;
        }
        public string[] ValidValues { get; }
    }

    public class WithPublicFields
    {
        public const int X = 1234;
        public int Age;

        public readonly string Type = "yes";

        private int number;

        public uint UNumber;

        public decimal Value { get; set; }

        public WithPublicFields(int age, int number)
        {
            Age = age;
            this.number = number;
            Value = 1.234m;
        }
    }

    public class WithNullable
    {
        public int? IntVal { get; set; }
        public Guid? GuidVal { get; set; }
    }

    public class Outer
    {
        public Inner Inner { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public bool Active { get; set; }
        public Guid Id { get; set; }
    }

    public class Inner
    {
        public MyEnum My { get; set; }
        public EmptyClass Empty { get; set; }
        public string EmptyString { get; set; }
        public EmptyClass MustBeEmpty { get; set; }
    }

    public enum MyEnum
    {
        Value0 = 0,
        Value1 = 1
    }

    public class EmptyClass
    {
        public string Name { get; set; }
    }

    public class ClassWithArrayAndIEnumerable
    {
        public string[] Values { get; set; }
        public IEnumerable<int> IntValues { get; set; }
    }


}
