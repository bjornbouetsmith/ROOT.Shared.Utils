using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Serialization;

namespace ROOT.Shared.Utils.Tests;

[TestClass]
public class TypeDumperTest
{
    private static TypeDumperTestSimple CreateSimple()
    {
        return new TypeDumperTestSimple
        {
            Name = "test",
            Count = 5,
            Enabled = true
        };
    }

    [TestMethod]
    public void GenericCreate_ForClass_ReturnsClassDumper()
    {
        var dumper = TypeDumper<TypeDumperTestSimple>.Create();

        Assert.IsInstanceOfType(dumper, typeof(ClassDumper<TypeDumperTestSimple>));
        Assert.AreSame(dumper, TypeDumper<TypeDumperTestSimple>.Create());
    }

    [TestMethod]
    public void GenericCreate_ForEnum_ReturnsEnumDumper()
    {
        var dumper = TypeDumper<TypeDumperTestEnum>.Create();

        Assert.IsInstanceOfType(dumper, typeof(EnumDumper<TypeDumperTestEnum>));
    }

    [TestMethod]
    public void GenericCreate_ForDateTime_ReturnsDateTimeDumper()
    {
        var dumper = TypeDumper<DateTime>.Create();

        Assert.IsInstanceOfType(dumper, typeof(DateTimeDumper));
    }

    [TestMethod]
    public void GenericCreate_ForPrimitive_ReturnsClassDumper()
    {
        // Only enums and DateTime get a dedicated dumper. Everything else is
        // handled by the general class dumper.
        var dumper = TypeDumper<int>.Create();

        Assert.IsInstanceOfType(dumper, typeof(ClassDumper<int>));
    }

    [TestMethod]
    public void GenericDump_ForClass_UsesSimpleFormatterByDefault()
    {
        var dumper = TypeDumper<TypeDumperTestSimple>.Create();

        var result = dumper.Dump(CreateSimple());

        Assert.AreEqual("Name:test|,Count:5|,Enabled:true|", result);
    }

    [TestMethod]
    public void GenericDump_ForClass_WithJsonFormatter()
    {
        var dumper = TypeDumper<TypeDumperTestSimple>.Create();

        var result = dumper.Dump(CreateSimple(), new JsonFormatter());

        Assert.AreEqual("{\"Name\":\"test\",\"Count\":5,\"Enabled\":true}", result);
    }

    [TestMethod]
    public void GenericDump_ForNestedClass_WritesAllMembers()
    {
        var value = new TypeDumperTestNested
        {
            Child = CreateSimple(),
            Kind = TypeDumperTestEnum.Second
        };

        var dumper = TypeDumper<TypeDumperTestNested>.Create();

        Assert.AreEqual(
            "Child:Name:test|,Count:5|,Enabled:true|,Kind:\"Second\"|",
            dumper.Dump(value));
        Assert.AreEqual(
            "{\"Child\":{\"Name\":\"test\",\"Count\":5,\"Enabled\":true},\"Kind\":\"Second\"}",
            dumper.Dump(value, new JsonFormatter()));
    }

    [TestMethod]
    public void GenericDump_ForEnum_ReturnsEnumName()
    {
        var dumper = TypeDumper<TypeDumperTestEnum>.Create();

        Assert.AreEqual("Second", dumper.Dump(TypeDumperTestEnum.Second));
        Assert.AreEqual("\"Second\"", dumper.Dump(TypeDumperTestEnum.Second, new JsonFormatter()));
        Assert.AreEqual(
            "\"Second\"",
            dumper.Dump(TypeDumperTestEnum.Second, new JsonFormatter(), new StringBuilder()).ToString());
    }

    [TestMethod]
    public void GenericDump_ForDateTime_UsesIso8601Format()
    {
        var value = DateTime.SpecifyKind(new DateTime(2019, 06, 06, 0, 0, 0), DateTimeKind.Utc);
        var dumper = TypeDumper<DateTime>.Create();

        Assert.AreEqual("2019-06-06T00:00:00.000Z", dumper.Dump(value));
    }

    [TestMethod]
    public void GenericDump_WithStringBuilder_AppendsAndReturnsSameBuilder()
    {
        var builder = new StringBuilder("prefix:");
        var dumper = TypeDumper<TypeDumperTestSimple>.Create();

        var result = dumper.Dump(CreateSimple(), new JsonFormatter(), builder);

        Assert.AreSame(builder, result);
        Assert.AreEqual("prefix:{\"Name\":\"test\",\"Count\":5,\"Enabled\":true}", builder.ToString());
    }

    [TestMethod]
    public void GenericDump_ForNullValue_ReturnsNull()
    {
        var dumper = TypeDumper<TypeDumperTestSimple>.Create();

        Assert.AreEqual("null", dumper.Dump((TypeDumperTestSimple)null));
        Assert.AreEqual("null", dumper.Dump(null, new JsonFormatter()));
        Assert.AreEqual("null", dumper.Dump(null, new JsonFormatter(), new StringBuilder()).ToString());
    }

    [TestMethod]
    public void GenericDump_ThroughBaseClassDumpObject()
    {
        TypeDumper dumper = TypeDumper<TypeDumperTestSimple>.Create();

        Assert.AreEqual("Name:test|,Count:5|,Enabled:true|", dumper.Dump(CreateSimple()));
    }

    [TestMethod]
    public void Create_ForType_ReturnsTypedDumper()
    {
        var dumper = TypeDumper.Create(typeof(TypeDumperTestSimple));

        Assert.IsInstanceOfType(dumper, typeof(ClassDumper<TypeDumperTestSimple>));
    }

    [TestMethod]
    public void Create_ForType_ReturnsSameInstanceAsGenericCreate()
    {
        var fromType = TypeDumper.Create(typeof(TypeDumperTestSimple));
        var fromGeneric = TypeDumper.Create<TypeDumperTestSimple>();

        Assert.AreSame(fromType, fromGeneric);
        Assert.AreSame(fromGeneric, TypeDumper<TypeDumperTestSimple>.Create());
    }

    [TestMethod]
    public void Create_ForType_CachesDumpersPerType()
    {
        var first = TypeDumper.Create(typeof(TypeDumperTestSimple));
        var second = TypeDumper.Create(typeof(TypeDumperTestSimple));
        var other = TypeDumper.Create(typeof(TypeDumperTestNested));

        Assert.AreSame(first, second);
        Assert.AreNotSame(first, other);
    }

    [TestMethod]
    public void Create_ForType_IsSafeToCallInParallel()
    {
        var dumpers = new TypeDumper[64];

        Parallel.For(0, dumpers.Length, i =>
        {
            dumpers[i] = TypeDumper.Create(typeof(TypeDumperTestNested));
        });

        Assert.IsTrue(dumpers.All(dumper => ReferenceEquals(dumper, dumpers[0])));
    }

    [TestMethod]
    public void Create_ForEnumType_ReturnsEnumDumper()
    {
        var dumper = TypeDumper.Create(typeof(TypeDumperTestEnum));

        Assert.IsInstanceOfType(dumper, typeof(EnumDumper<TypeDumperTestEnum>));
        Assert.AreEqual("Second", dumper.Dump(TypeDumperTestEnum.Second));
    }

    [TestMethod]
    public void Create_ForDateTimeType_ReturnsDateTimeDumper()
    {
        var value = DateTime.SpecifyKind(new DateTime(2019, 06, 06, 0, 0, 0), DateTimeKind.Utc);
        var dumper = TypeDumper.Create(typeof(DateTime));

        Assert.IsInstanceOfType(dumper, typeof(DateTimeDumper));
        Assert.AreEqual("2019-06-06T00:00:00.000Z", dumper.Dump(value));
    }

    [TestMethod]
    public void Create_ForNullType_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => TypeDumper.Create(null));
    }

    [TestMethod]
    public void DumpObject_ForClass_UsesSimpleFormatter()
    {
        var dumper = TypeDumper.Create(typeof(TypeDumperTestSimple));

        Assert.AreEqual("Name:test|,Count:5|,Enabled:true|", dumper.Dump(CreateSimple()));
    }

    [TestMethod]
    public void DumpObject_ForPrimitive_WritesValue()
    {
        Assert.AreEqual("42", TypeDumper.Create(typeof(int)).Dump(42));
        Assert.AreEqual("hello", TypeDumper.Create(typeof(string)).Dump("hello"));
    }

    [TestMethod]
    public void DumpObject_ForNull_ReturnsNull()
    {
        var dumper = TypeDumper.Create(typeof(TypeDumperTestSimple));

        Assert.AreEqual("null", dumper.Dump(null));
    }

    #region Expression helpers

    [TestMethod]
    public void ForEach_IteratesCollection()
    {
        var builder = Expression.Parameter(typeof(StringBuilder), "builder");
        var loopVar = Expression.Parameter(typeof(int), "item");
        var append = Expression.Call(
            builder,
            typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), new[] { typeof(int) }),
            loopVar);

        var body = Expression.Block(
            TypeDumper.ForEach(Expression.Constant(new[] { 1, 2, 3 }), loopVar, append),
            builder);

        var action = Expression.Lambda<Func<StringBuilder, StringBuilder>>(body, builder).Compile();

        Assert.AreEqual("123", action(new StringBuilder()).ToString());
    }

    [TestMethod]
    public void For_IteratesRange()
    {
        var builder = Expression.Parameter(typeof(StringBuilder), "builder");
        var loopVar = Expression.Parameter(typeof(int), "i");
        var append = Expression.Call(
            builder,
            typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), new[] { typeof(int) }),
            loopVar);

        var loop = TypeDumper.For(
            loopVar,
            Expression.Constant(0),
            Expression.LessThan(loopVar, Expression.Constant(3)),
            Expression.AddAssign(loopVar, Expression.Constant(1)),
            append);

        var body = Expression.Block(loop, builder);
        var action = Expression.Lambda<Func<StringBuilder, StringBuilder>>(body, builder).Compile();

        Assert.AreEqual("012", action(new StringBuilder()).ToString());
    }

    #endregion
}

public enum TypeDumperTestEnum
{
    First = 0,
    Second = 1
}

public class TypeDumperTestSimple
{
    public string Name { get; set; }
    public int Count { get; set; }
    public bool Enabled { get; set; }
}

public class TypeDumperTestNested
{
    public TypeDumperTestSimple Child { get; set; }
    public TypeDumperTestEnum Kind { get; set; }
}