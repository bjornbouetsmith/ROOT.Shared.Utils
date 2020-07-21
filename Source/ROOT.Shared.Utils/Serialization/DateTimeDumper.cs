using System;
using System.Globalization;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class DateTimeDumper : TypeDumper<DateTime>
    {
        private static readonly string Format = "yyyy-MM-ddTHH:mm:ss.fffK";
        public override string Dump(object what)
        {
            return Dump((DateTime)what); ;
        }

        public override string Dump(DateTime what)
        {
            return what.ToString(Format, CultureInfo.InvariantCulture);
        }

        public override string Dump(DateTime what, IFormatter formatter)
        {
            var sb = new StringBuilder();

            Dump(what, formatter, sb);

            return sb.ToString();
        }

        public override StringBuilder Dump(DateTime what, IFormatter formatter, StringBuilder target)
        {
            formatter.WriteValue(what, target);
            return target;
        }
    }
}
