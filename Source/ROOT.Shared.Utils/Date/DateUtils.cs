using ROOT.Shared.Utils.Serialization;
using System;
using System.Globalization;

namespace ROOT.Shared.Utils.Date
{
    public static class DateUtils
    {
        private static readonly string Iso8601Date = "yyyy-MM-dd";
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTimeDumper DateTimeDumper = new DateTimeDumper();
        public static DateTime ToDateTime(long timeSinceEpoch)
        {
            return Epoch.AddSeconds(timeSinceEpoch);
        }

        public static string ToIso8601DateTimeString(this DateTime dt)
        {
            return DateTimeDumper.Dump(dt);
        }

        public static string ToIso8601DateString(this DateTime dt)
        {
            return dt.ToString(Iso8601Date, CultureInfo.InvariantCulture);
        }


    }
}
