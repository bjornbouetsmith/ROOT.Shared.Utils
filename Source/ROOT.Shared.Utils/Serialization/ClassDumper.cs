using System;
using System.Linq.Expressions;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
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
}