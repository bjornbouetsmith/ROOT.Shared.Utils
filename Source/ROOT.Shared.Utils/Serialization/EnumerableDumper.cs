using System;
using System.Collections.Generic;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class EnumerableDumper<T> : TypeDumper<IEnumerable<T>>
    {
        public override string Dump(object what)
        {
            return Dump((IEnumerable<T>)what);
        }

        public override string Dump(IEnumerable<T> what)
        {
            return Dump(what, new SimpleFormatter());
        }

        public override string Dump(IEnumerable<T> what, IFormatter formatter)
        {
            if (Equals(what, null))
            {
                return "null";
            }

            var builder = new StringBuilder();

            Dump(what, formatter, builder);

            return builder.ToString();
        }

        public override StringBuilder Dump(IEnumerable<T> what, IFormatter formatter, StringBuilder target)
        {
            if (Equals(what, default(T)))
            {
                target.Append("null");

            }

            var dumper = TypeDumper.Create<T>();
            formatter.BeginArray(target);
            foreach (var val in what)
            {
                dumper.Dump(val, formatter, target);
                formatter.WriteArrayValueSep(target);
            }
            formatter.EndArray(target);


            return target;
        }
    }
}
