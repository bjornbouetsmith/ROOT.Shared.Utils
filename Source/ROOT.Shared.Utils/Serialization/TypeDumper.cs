﻿using System;
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
        public abstract string Dump(T what, IFormatter formatter);
        public abstract StringBuilder Dump(T what, IFormatter formatter,StringBuilder target);
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

    

        private static Expression IsNull(Expression what)
        {
            var method = typeof(object).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(mi => mi.Name == nameof(object.Equals) && mi.GetParameters().Length == 2);

            return Expression.Call(method, Expression.Constant(null, typeof(object)), Expression.Convert(what, typeof(object)));
        }
        
        private static Expression GetDumperAndDump(Expression formatter, Expression builder, Expression value, Type valueType)
        {
            var genericDumpertype = typeof(TypeDumper<>).MakeGenericType(valueType);
            var dumper = Expression.Convert(Expression.Call(GetDumperMethod, Expression.Constant(valueType, typeof(Type))), genericDumpertype);

            var method = genericDumpertype.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(mi => mi.Name == nameof(Dump) && mi.GetParameters().Length == 3 
                                                              && mi.GetParameters()[0].ParameterType == valueType 
                                                              && mi.GetParameters()[1].ParameterType == typeof(IFormatter)
                                                              && mi.GetParameters()[2].ParameterType == typeof(StringBuilder));
            return Expression.Call(dumper, method, value, formatter, builder);
        }

        internal static Expression GetFullObjDump(ParameterExpression builder, ParameterExpression formatter, Expression what, Type whatType)
        {
            Debug.WriteLine($"Creating dumper for type:{whatType.FullName}");

            return WriteObject(builder, formatter, what, whatType);
        }

        private static MethodInfo GetValueFormatterMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.Get));
        private static readonly MethodInfo GetBeginObjectMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.BeginObject));
        private static readonly MethodInfo GetEndObjectMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.EndObject));

        private static readonly MethodInfo GetBeginFieldMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.BeginField));
        private static readonly MethodInfo GetEndFieldMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.EndField));


        internal static Expression WriteObject(ParameterExpression builder, ParameterExpression formatter, Expression what, Type whatType)
        {

            if (whatType.IsPrimitive 
                || whatType == typeof(string)
                || whatType == typeof(DateTime))
            {
                var realFormatterMethod = GetValueFormatterMethod.MakeGenericMethod(whatType);
                var typeFormattertype = typeof(ITypeFormatter<>).MakeGenericType(whatType);
                var typeFormatterMethod = typeFormattertype.GetMethod(nameof(ITypeFormatter<string>.Write));

                var typeFormatter = Expression.Call(formatter, realFormatterMethod);
                return Expression.Call(typeFormatter, typeFormatterMethod, what, builder);
            }

            if (whatType.IsArray || whatType.GetInterfaces().Contains(typeof(IEnumerable)))
            {
                return WriteArrayType(builder, formatter, what, whatType);
            }


            List<Expression> expressions = new List<Expression>();
            
            var props = whatType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.CanRead && pi.CanWrite);

            expressions.Add(Expression.Call(formatter, GetBeginObjectMethod, builder));

            foreach (var prop in props)
            {
                expressions.Add(Expression.Call(formatter, GetBeginFieldMethod, Expression.Constant(prop.Name, typeof(string)), builder));
                var val = Expression.PropertyOrField(what, prop.Name);
                if (prop.PropertyType.Namespace == typeof(string).Namespace)
                {
                    // Built in simple types

                    expressions.Add(GetFullObjDump(builder, formatter, val, prop.PropertyType));
                }
                else
                {
                    expressions.Add(GetDumperAndDump(formatter, builder, val, prop.PropertyType));
                }

                expressions.Add(Expression.Call(formatter, GetEndFieldMethod, Expression.Constant(prop.Name, typeof(string)), builder));
            }

            if (expressions.Count > 1)
            {
                expressions.RemoveAt(expressions.Count - 1);
            }

            expressions.Add(Expression.Call(formatter, GetEndObjectMethod, builder));

            return Expression.Block(expressions);
        }

        private static readonly MethodInfo GetBeginArrayMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.BeginArray));
        private static readonly MethodInfo GetEndArrayMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.EndArray));
        private static readonly MethodInfo GetWriteArrayValueSepMethod = typeof(IFormatter).GetMethod(nameof(IFormatter.WriteArrayValueSep));



        internal static Expression WriteArrayType(ParameterExpression builder, ParameterExpression formatter, Expression what, Type whatType)
        {
            var contained = whatType.GetElementType();
            if (contained == typeof(XElement))
            {
                return Expression.Empty();
            }

            if (contained == null)
            {
                // Probably generic list or something similar
                if (whatType.IsGenericType)
                {
                    contained = whatType.GetGenericArguments()[0];
                }
            }

            List<Expression> expressions = new List<Expression>();

            expressions.Add(Expression.Call(formatter, GetBeginArrayMethod, builder));

            var loopVar = Expression.Parameter(contained, "loopVar");

            var loopBody =Expression.Block( WriteObject(builder, formatter, loopVar, contained),Expression.Call(formatter, GetWriteArrayValueSepMethod,builder));
            var loop = ForEach(what, loopVar, loopBody);
            var printIfNotNull = Expression.IfThenElse(
                IsNull(what),
                Expression.Empty(),
                loop);

            expressions.Add(printIfNotNull);

            expressions.Add(Expression.Call(formatter, GetEndArrayMethod, builder));

            return Expression.Block(expressions);
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
                Debug.WriteLine($"Creating object dumper for type: {t.FullName}");
                var dumperType = typeof(TypeDumper<>).MakeGenericType(t);

                var method = dumperType.GetMethod(nameof(TypeDumper<int>.Create), BindingFlags.Public | BindingFlags.Static);

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