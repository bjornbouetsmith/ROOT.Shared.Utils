using System;
using System.Globalization;

namespace ROOT.Shared.Utils.Serialization
{
    public static class StringFormatterUtils
    {
        private static readonly DateTimeDumper DateTimeDumper = new DateTimeDumper();
        public static string AsString(this DateTime dt)
        {
            return DateTimeDumper.Dump(dt);
        }

        public static string AsString(this long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string AsString(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string AsString(this short value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string AsString(this byte value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string AsString(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string AsString(this float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string AsString(this decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string AsString(this bool value)
        {
            return value ? "True" : "False";
        }

        public static string AsString<T>(this T value)
        where T : Enum
        {
            return EnumDumpers<T>.Instance.Dump(value);
        }

        public static string AsString(this TimeSpan value)
        {
            if (value.Ticks < TimeSpan.TicksPerMillisecond)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} ticks", value.Ticks.AsString());
            }

            if (value < TimeSpan.FromSeconds(1))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}ms", value.TotalMilliseconds.AsString());
            }

            if (value < TimeSpan.FromMinutes(1))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}s", value.TotalSeconds.AsString());
            }

            if (value < TimeSpan.FromHours(1))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}m", value.TotalMinutes.AsString());
            }

            if (value < TimeSpan.FromDays(1))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}hours", value.TotalHours.AsString());
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} days, {1} hours, {2}mins", value.Days.AsString(), value.Hours.AsString(), value.Minutes.AsString());
        }

    }
}
