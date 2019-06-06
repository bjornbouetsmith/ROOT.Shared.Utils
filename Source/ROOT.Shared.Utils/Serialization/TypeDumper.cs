using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace ROOT.Shared.Utils.Serialization
{
    public abstract class TypeDumper<T> : TypeDumper
    {
        private static TypeDumper<T> _instance;

        public static TypeDumper<T> Create()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = CreateDumper();

            return _instance;
        }

        private static TypeDumper<T> CreateDumper()
        {
            if (typeof(T).IsEnum)
            {
                return new EnumDumper<T>();
            }

            if (typeof(T) == typeof(DateTime))
            {
                return (TypeDumper<T>)(object)new DateTimeDumper();
            }

            return new ClassDumper<T>();
        }

        public abstract string Dump(T what);
    }

    public abstract class TypeDumper
    {
        protected static readonly MethodInfo ObjToString = typeof(object).GetMethod(nameof(ToString), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

        protected static readonly MethodInfo Append = typeof(StringBuilder).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(
            mi =>
                mi.Name == nameof(StringBuilder.Append)
                && mi.GetParameters().Length == 1
                && mi.GetParameters()[0].ParameterType == typeof(string));
        protected static readonly MethodInfo GetDumperMethod = typeof(TypeDumper).GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(mi => mi.Name == nameof(TypeDumper.Create) && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == typeof(Type));

        internal static Expression GetDump(Expression what, ParameterExpression builder, Type fieldType, string name)
        {
            Expression field = Expression.Constant(name, typeof(string));
            Expression sep = Expression.Constant(":", typeof(string));
            Expression val = Expression.PropertyOrField(what, name);
            List<Expression> list = new List<Expression>();
            list.Add(Expression.Call(builder, Append, field));
            list.Add(Expression.Call(builder, Append, sep));
            Debug.WriteLine("Creating dumper for field/property:{0}", name);
            if (fieldType.IsValueType)
            {
                var toString = fieldType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(mi => mi.Name == nameof(ToString) && mi.GetParameters().Length == 0);

                if (toString != null)
                {
                    val = Expression.Call(val, toString);
                }
                else
                {
                    val = Expression.Call(Expression.Convert(val, typeof(object)), ObjToString);
                }

                var appendVal = Expression.Call(builder, Append, val);
                list.Add(appendVal);
                return Expression.Block(list);
            }

            var append = Expression.Call(builder, Append, DumpValue(val, fieldType));

            list.Add(append);
            return Expression.Block(list);
        }

        private static Expression IsNull(Expression what)
        {
            var method = typeof(object).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(mi => mi.Name == nameof(object.Equals) && mi.GetParameters().Length == 2);

            return Expression.Call(method, Expression.Constant(null, typeof(object)), Expression.Convert(what, typeof(object)));
        }

        private static Expression DumpValue(Expression value, Type valueType)
        {
            var genericDumpertype = typeof(TypeDumper<>).MakeGenericType(valueType);
            var dumper = Expression.Convert(Expression.Call(GetDumperMethod, Expression.Constant(valueType, typeof(Type))), genericDumpertype);

            var method = genericDumpertype.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(mi => mi.Name == nameof(Dump) && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == valueType);
            return Expression.Call(dumper, method, value);
        }

        internal static Expression GetObjDump(ParameterExpression builder, Expression what, Type whatType)
        {
            Debug.WriteLine("Creating dumper for type:{0}", whatType.FullName);
            if (whatType == typeof(string))
            {
                return Expression.Call(builder, Append, what);
            }

            if (whatType.IsArray)
            {
                var contained = whatType.GetElementType();
                if (contained == typeof(XElement))
                {
                    return Expression.Empty();
                }
                var loopVar = Expression.Parameter(contained, "loopVar");
                var loopBody = Expression.Call(builder, Append, DumpValue(loopVar, contained));
                var loop = ForEach(what, loopVar, loopBody);

                var printIfNotNull = Expression.IfThenElse(
                    IsNull(what),
                    Expression.Empty(),
                    loop);


                return printIfNotNull;
            }

            var props = whatType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.CanRead && pi.CanWrite);
            List<Expression> list = new List<Expression>();
            foreach (var prop in props)
            {
                list.Add(GetDump(what, builder, prop.PropertyType, prop.Name));
                list.Add(Expression.Call(builder, Append, Expression.Constant(", ", typeof(string))));
            }

            if (list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
                return Expression.Block(list);
            }

            return Expression.Empty();
        }
        public static Expression ForEach(Expression collection, ParameterExpression loopVar, Expression loopContent)
        {
            var elementType = loopVar.Type;
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

            var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator"));
            var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

            // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

            var breakLabel = Expression.Label("LoopBreak");

            var loop = Expression.Block(new[] { enumeratorVar },
                enumeratorAssign,
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Equal(moveNextCall, Expression.Constant(true)),
                        Expression.Block(new[] { loopVar },
                            Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
                            loopContent
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel)
            );


            return loop;
        }

        public static Expression For(ParameterExpression loopVar, Expression initValue, Expression condition, Expression increment, Expression loopContent)
        {
            var initAssign = Expression.Assign(loopVar, initValue);

            var breakLabel = Expression.Label("LoopBreak");

            var loop = Expression.Block(new[] { loopVar },
                initAssign,
                Expression.Loop(
                    Expression.IfThenElse(
                        condition,
                        Expression.Block(
                            loopContent,
                            increment
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel)
            );

            return loop;
        }

        private static readonly Dictionary<Type, TypeDumper> Dumpers = new Dictionary<Type, TypeDumper>();

        public static TypeDumper Create(Type t)
        {
            if (!Dumpers.TryGetValue(t, out var dumper))
            {
                Debug.WriteLine("Creating object dumper for type: {0}", t.FullName);
                var dumperType = typeof(TypeDumper<>).MakeGenericType(t);

                var method = dumperType.GetMethod(nameof(Create), BindingFlags.Public | BindingFlags.Static);

                dumper = (TypeDumper)method.Invoke(null, null);

                Dumpers[t] = dumper;
            }

            return dumper;
        }

        public static TypeDumper<T> Create<T>()
        {
            return (TypeDumper<T>)Create(typeof(T));
        }

        public abstract string Dump(object what);
    }
}