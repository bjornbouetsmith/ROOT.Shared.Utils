using System;
using System.Collections.Generic;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class DateTimeDumper : TypeDumper<DateTime>
    {
        private static readonly string Format = "yyyy-MM-dd hh:mm:ss.fff";
        public override string Dump(object what)
        {
            return Dump((DateTime)what); ;
        }

        public override string Dump(DateTime what)
        {
            return what.ToString(Format);
        }
    }
}
