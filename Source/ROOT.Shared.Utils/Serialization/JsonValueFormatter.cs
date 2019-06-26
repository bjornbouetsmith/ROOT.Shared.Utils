using System;
using System.Globalization;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class JsonValueFormatter : IValueFormatter
    {
        private static readonly string Format = "yyyy-MM-ddThh:mm:ss.fffZ";

        public void WriteNumber<T>(T number, StringBuilder target)
            where T : struct, IConvertible, IFormattable
        {
            target.Append(number.ToString(CultureInfo.InvariantCulture));
        }

        public void Write(string value, StringBuilder target)
        {
            target.Append("\"");
            target.Append(value);
            target.Append("\"");
        }

        public void Write(DateTime value, StringBuilder target)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                target.Append(value.ToString(Format));
            }
            else
            {
                target.Append(value.ToUniversalTime().ToString(Format));
            }
        }

        public void Write(char value, StringBuilder target)
        {
            target.Append("\"");
            target.Append(value);
            target.Append("\"");
        }

        public void Write(byte value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(short value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(int value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(long value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(float value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(double value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(decimal value, StringBuilder target)
        {
            WriteNumber(value, target);
        }
    }
}