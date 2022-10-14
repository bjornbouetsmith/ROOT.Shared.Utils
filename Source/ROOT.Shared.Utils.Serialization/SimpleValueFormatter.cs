using System;
using System.Globalization;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public abstract class ValueFormatter
    {
        protected static readonly string Format = "yyyy-MM-ddThh:mm:ss.fffZ";

        public void WriteNumber<T>(T number, StringBuilder target)
            where T : struct, IConvertible, IFormattable
        {
            target.Append(number.ToString(CultureInfo.InvariantCulture));
        }

        public void Write(bool value, StringBuilder target)
        {
            if (value)
            {
                target.Append("true");
            }
            else
            {
                target.Append("false");
            }
        }

        public virtual void Write(Guid value, StringBuilder target)
        {
            target.Append(value);
        }
    }

    public class SimpleValueFormatter : ValueFormatter, IValueFormatter
    {
        public void Write(string value, StringBuilder target)
        {
            target.Append(value);
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
            target.Append(value);
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

        public void Write(ushort value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(uint value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(ulong value, StringBuilder target)
        {
            WriteNumber(value, target);
        }

        public void Write(TimeSpan value, StringBuilder target)
        {
            WriteNumber(value.Ticks, target);
        }
    }
}
