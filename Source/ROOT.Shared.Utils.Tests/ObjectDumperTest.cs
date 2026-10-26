using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Serialization;

namespace ROOT.Shared.Utils.Tests;

[TestClass]
public class ObjectDumperTest
{
    [TestMethod]
    public void ClassWithArray()
    {
        var data = new WithArrayOfStrings("first", "second");

        var content = data.Dump(new JsonFormatter());

        Assert.AreEqual("{\"ValidValues\":[\"first\",\"second\"]}", content);
    }

    [TestMethod]
    public void EnumerableIntDump()
    {
        IEnumerable<int> vals = new[] {1, 2, 3};

            
        var content = vals.Dump(new JsonFormatter());
        Assert.AreEqual("[1,2,3]", content);
    }
    [TestMethod]
    public void EnumerableClassDump()
    {
        IEnumerable<WithPublicFields> vals = new[] { new WithPublicFields(1, 2),new WithPublicFields(3,4) };

        var str = vals.Dump(new JsonFormatter());
        Assert.AreEqual("[{\"Value\":1.234,\"Age\":1,\"Type\":\"yes\",\"UNumber\":0},{\"Value\":1.234,\"Age\":3,\"Type\":\"yes\",\"UNumber\":0}]", str);
            
    }

    [TestMethod]
    public void SimpleDump()
    {
        var expected = "Hello world";
        var str = expected.Dump();
        Assert.AreEqual(expected, str);
    }

    [TestMethod]
    public void DateTimeDump()
    {
        var dt = new DateTime(2019, 06, 06, 0, 0, 0);
        dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        string expected = "2019-06-06T00:00:00.000Z";
        var str = dt.Dump();
        Assert.AreEqual(expected, str);
    }

    [TestMethod]
    public void SimpleClass()
    {
        var outer = new Outer();
        outer.Inner = new Inner { Empty = new EmptyClass(), My = MyEnum.Value1 };

        var str = outer.Dump();

        Assert.AreEqual("Inner:My:\"Value1\"|,Empty|,Price:0|,Active:false|,Id:00000000-0000-0000-0000-000000000000|", str);
    }

    [TestMethod]
    public void SimpleClassWithFormatter()
    {
        var outer = new Outer();
        outer.Name = "bjørn";
        outer.Price = 1.2345d;
        outer.Id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        outer.Inner = new Inner { Empty = new EmptyClass(), My = MyEnum.Value1 };

        var str = outer.Dump(new JsonFormatter());

        Assert.AreEqual("{\"Inner\":{\"My\":\"Value1\",\"Empty\":}},\"Name\":\"bjørn\",\"Price\":1.2345,\"Active\":false,\"Id\":\"11111111-2222-3333-4444-555555555555\"}", str);
    }

    [TestMethod]
    public void EmptyClassWithoutPropertiesTest()
    {
        var empty = new EmptyClass();

        var str = empty.Dump();
        Assert.AreEqual(string.Empty, str);
    }

    [TestMethod]
    public void EmptyClassWithoutPropertiesTestNull()
    {
        var str = ObjectDumper.Dump<EmptyClass>(null);
        Assert.AreEqual("null", str);
    }

    [TestMethod]
    public void ArrayTypes()
    {
        var withArr = new ClassWithArrayAndIEnumerable();
        withArr.Values = new[] { "First", "Secpmd" };
        withArr.IntValues = new List<int> { 1, 2, 3 };

        var str = withArr.Dump(new SimpleFormatter());
        Assert.AreEqual("Values:[First,Secpmd]|,IntValues:[1,2,3]|", str);
    }

    [TestMethod]
    public void WithNullableTest()
    {
        var empty = new WithNullable();
        empty.GuidVal = Guid.Empty;
        var str = empty.Dump();
        Assert.AreEqual("GuidVal:00000000-0000-0000-0000-000000000000|", str);

        var json = empty.Dump(new JsonFormatter());
        Assert.AreEqual("{\"GuidVal\":\"00000000-0000-0000-0000-000000000000\"}", json);
    }

    [TestMethod]
    public void WithPublicFields()
    {
        var obj = new WithPublicFields(46, 42);
        obj.UNumber = 123;

        Assert.AreEqual("Value:1.234|,Age:46|,Type:yes|,UNumber:123|", obj.Dump());
        Assert.AreEqual("{\"Value\":1.234,\"Age\":46,\"Type\":\"yes\",\"UNumber\":123}", obj.Dump(new JsonFormatter()));
    }

    [TestMethod]
    public void DictionaryTest()
    {
        var withDic = new WithDictionary();
        withDic.Data = new Dictionary<string, WithNullable>();
        withDic.Data["test"] = new WithNullable { GuidVal = Guid.Empty };

        Assert.AreEqual("Data:test:GuidVal:00000000-0000-0000-0000-000000000000|", withDic.Dump());
        Assert.AreEqual("{\"Data\":{\"test\":{\"GuidVal\":\"00000000-0000-0000-0000-000000000000\"}}}", withDic.Dump(new JsonFormatter()));
    }

    [TestMethod]
    public void TimeSpanTest()
    {
        var withTimeSpan = new WithTimeSpan { ReadLatency = TimeSpan.FromMilliseconds(0.01), WriteLatency = TimeSpan.FromMilliseconds(0.1) };
        Assert.AreEqual("ReadLatency:100|,WriteLatency:1000|", withTimeSpan.Dump());
        Assert.AreEqual("{\"ReadLatency\":100,\"WriteLatency\":1000}", withTimeSpan.Dump(new JsonFormatter()));
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

public class WithTimeSpan
{
    public TimeSpan ReadLatency { get; set; }
    public TimeSpan WriteLatency { get; set; }
}