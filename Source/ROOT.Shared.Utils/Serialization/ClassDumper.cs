using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class ClassDumper<T> : TypeDumper<T>
    {
        private static readonly Action<T, StringBuilder> _dumper;
        static ClassDumper()
        {
            try
            {
                ParameterExpression builder = Expression.Parameter(typeof(StringBuilder), "builder");
                ParameterExpression what = Expression.Parameter(typeof(T), "what");

                var body = Expression.Block(typeof(void), GetObjDump(builder, what, typeof(T)));

                var parameterExpression = Expression.Parameter(typeof(Exception), "ex");

                Expression writeEx = Expression.Block(typeof(void), Expression.Call(builder, Append, Expression.Call(Expression.Convert(parameterExpression, typeof(object)), ObjToString)));

                var tryCatch = Expression.TryCatch(body, Expression.MakeCatchBlock(typeof(Exception), parameterExpression, writeEx, null));
                
                var actionExp = Expression.Lambda<Action<T, StringBuilder>>(tryCatch, what, builder);

                _dumper = actionExp.Compile();
            }
            catch (Exception e)
            {
                _dumper = (to, sb) => sb.Append(e.ToString());
            }
        }

        public override string Dump(T what)
        {
            if (Equals(what, default(T)))
            {
                return string.Empty;
            }

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
}