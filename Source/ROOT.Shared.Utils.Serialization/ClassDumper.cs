using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class ClassDumper<T> : TypeDumper<T>
    {
        private static readonly Action<T, StringBuilder, IFormatter> _fullDumper;

        static ClassDumper()
        {
            try
            {
                ParameterExpression builder = Expression.Parameter(typeof(StringBuilder), "builder");
                ParameterExpression what = Expression.Parameter(typeof(T), "what");
                ParameterExpression formatter = Expression.Parameter(typeof(IFormatter), "formatter");
                
                var fullBody = Expression.Block(typeof(void), GetFullObjDump(builder, formatter, what, typeof(T)));

                var parameterExpression = Expression.Parameter(typeof(Exception), "ex");

                Expression writeEx = Expression.Block(typeof(void), Expression.Call(builder, Append, Expression.Call(Expression.Convert(parameterExpression, typeof(object)), ObjToString)));

                var fullTryCatch = Expression.TryCatch(fullBody, Expression.MakeCatchBlock(typeof(Exception), parameterExpression, writeEx, null));

                var fullActionExp = Expression.Lambda<Action<T, StringBuilder, IFormatter>>(fullTryCatch, what, builder, formatter);

                
                _fullDumper = fullActionExp.Compile();
            }
            catch (Exception e)
            {
                
                _fullDumper = (to, sb, fmt) => sb.Append(e.ToString());
            }
        }

        public override string Dump(T what)
        {
            return Dump(what, new SimpleFormatter());
        }

        public override string Dump(object what)
        {
            return Dump((T)what, new SimpleFormatter());
        }

        public override string Dump(T what, IFormatter formatter)
        {
            if (Equals(what, default(T)))
            {
                return "null";
            }

            var builder = new StringBuilder();

            Dump(what, formatter, builder);

            return builder.ToString();
        }

        public override StringBuilder Dump(T what, IFormatter formatter, StringBuilder target)
        {
            if (Equals(what, default(T)))
            {
                target.Append("null");
            }
            else
            {
                _fullDumper(what, target, formatter);
            }

            return target;
        }
    }
}