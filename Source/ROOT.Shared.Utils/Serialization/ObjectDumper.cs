using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public static class ObjectDumper
    {
        public static string Dump(this object what)
        {
            return TypeDumper.Create(what.GetType()).Dump(what);
        }

        public static string Dump<T>(this T what)
        {
            return TypeDumper.Create<T>().Dump(what);
        }
    }

    public class EnumDumper<T> : TypeDumper<T>
    {
        public override string Dump(T value)
        {
            return value.ToString();
        }

        public override string Dump(object what)
        {
            return Dump((T)what);
        }
    }

    public class ClassDumper<T> : TypeDumper<T>
    {
        private static readonly Action<T, StringBuilder> _dumper;

        static ClassDumper()
        {
           
            ParameterExpression builder = Expression.Parameter(typeof(StringBuilder), "builder");
            ParameterExpression what = Expression.Parameter(typeof(T), "what");

            var body = GetObjDump(builder, what, typeof(T));

            var actionExp = Expression.Lambda<Action<T, StringBuilder>>(body, what, builder);

            _dumper = actionExp.Compile();
        }

        public override string Dump(T what)
        {
            var builder = new StringBuilder();
            _dumper(what, builder);
            return builder.ToString();
        }

        public override string Dump(object what)
        {
            var builder = new StringBuilder();
            _dumper((T)what, builder);
            return builder.ToString();
        }
    }

    public abstract class TypeDumper
    {
        static readonly MethodInfo _objToString = typeof(object).GetMethod(nameof(ToString), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

        private static readonly MethodInfo _append = typeof(StringBuilder).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(
            mi =>
                mi.Name == nameof(StringBuilder.Append)
                && mi.GetParameters().Length == 1
                && mi.GetParameters()[0].ParameterType == typeof(string));

        internal static Expression GetDump(Expression what, ParameterExpression builder, Type fieldType, string name)
        {
            Expression field = Expression.Constant(name, typeof(string));
            Expression sep = Expression.Constant(":", typeof(string));
            Expression val = Expression.PropertyOrField(what, name);
            List<Expression> list = new List<Expression>();
            list.Add(Expression.Call(builder, _append, field));
            list.Add(Expression.Call(builder, _append, sep));

            if (fieldType.IsValueType)
            {
                var toString = fieldType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(mi => mi.Name == nameof(ToString) && mi.GetParameters().Length == 0);

                if (toString != null)
                {

                    val = Expression.Call(val, toString);
                }
                else
                {
                    val = Expression.Call(Expression.Convert(val, typeof(object)), _objToString);
                }

                list.Add(Expression.Call(builder, _append, val));
                return Expression.Block(list);
            }

            var objDump = GetObjDump(builder, val, fieldType);
            list.Add(objDump);
            return Expression.Block(list);
        }

        internal static Expression GetObjDump(ParameterExpression builder, Expression what, Type whatType)
        {
            if (whatType == typeof(string))
            {
                return Expression.Call(builder, _append, what);
            }

            var props = whatType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<Expression> list = new List<Expression>();
            foreach (var prop in props)
            {
                list.Add(GetDump(what, builder, prop.PropertyType, prop.Name));
                list.Add(Expression.Call(builder, _append, Expression.Constant(", ", typeof(string))));
            }

            list.RemoveAt(list.Count - 1);

            return Expression.Block(list);
        }

        private static readonly Dictionary<Type, TypeDumper> Dumpers = new Dictionary<Type, TypeDumper>();
    
        public static TypeDumper Create(Type t)
        {
            if (!Dumpers.TryGetValue(t, out var dumper))
            {
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

            return new ClassDumper<T>();
        }

        public abstract string Dump(T what);
    }
}
