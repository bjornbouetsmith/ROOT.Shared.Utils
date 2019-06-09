using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class ClassDumper<T> : TypeDumper<T>
    {
        private static readonly Action<T, StringBuilder> _simpleDumper;
        private static readonly Action<T, StringBuilder, IFormatter> _fullDumper;

        static ClassDumper()
        {
            try
            {
                ParameterExpression builder = Expression.Parameter(typeof(StringBuilder), "builder");
                ParameterExpression what = Expression.Parameter(typeof(T), "what");
                ParameterExpression formatter = Expression.Parameter(typeof(IFormatter), "formatter");

                var body = Expression.Block(typeof(void), GetObjDump(builder, what, typeof(T)));
                var fullBody = Expression.Block(typeof(void), GetFullObjDump(builder, formatter, what, typeof(T)));

                var parameterExpression = Expression.Parameter(typeof(Exception), "ex");

                Expression writeEx = Expression.Block(typeof(void), Expression.Call(builder, Append, Expression.Call(Expression.Convert(parameterExpression, typeof(object)), ObjToString)));

                var tryCatch = Expression.TryCatch(body, Expression.MakeCatchBlock(typeof(Exception), parameterExpression, writeEx, null));
                
                var actionExp = Expression.Lambda<Action<T, StringBuilder>>(tryCatch, what, builder);

                _simpleDumper = actionExp.Compile();

                var fullTryCatch = Expression.TryCatch(fullBody, Expression.MakeCatchBlock(typeof(Exception), parameterExpression, writeEx, null));

                var fullActionExp = Expression.Lambda<Action<T, StringBuilder,IFormatter>>(fullTryCatch, what, builder,formatter);

                _simpleDumper = actionExp.Compile();
                _fullDumper = fullActionExp.Compile();
            }
            catch (Exception e)
            {
                _simpleDumper = (to, sb) => sb.Append(e.ToString());
                _fullDumper = (to, sb,fmt) => sb.Append(e.ToString());
            }
        }

        public override string Dump(T what)
        {
            if (Equals(what, default(T)))
            {
                return "null";
            }

            var builder = new StringBuilder();
            _simpleDumper(what, builder);
            return builder.ToString();
        }

        public override string Dump(object what)
        {
            var builder = new StringBuilder();
            _simpleDumper((T)what, builder);
            return builder.ToString();
        }

        public override string Dump(T what, IFormatter formatter)
        {
            if (Equals(what, default(T)))
            {
                return "null";
            }

            var builder = new StringBuilder();
            _fullDumper(what, builder,formatter);
            return builder.ToString();
        }
    }
}