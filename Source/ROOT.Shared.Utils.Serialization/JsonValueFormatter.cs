using System;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class JsonNullableFormatter<T> : ITypeFormatter<T?>
        where T : struct
    {
        public void Write(T? value, StringBuilder target)
        {
            if (value.HasValue)
            {
                JsonFormatter<T>.Instance.Write(value.Value, target);
            }
            else
            {
                target.Append("null");
            }
        }
    }

    public class JsonValueFormatter : ValueFormatter, IValueFormatter
    {
        public void Write(string value, StringBuilder target)
        {
            target.Append("\"");
            target.Append(value);
            target.Append("\"");
        }
        public override void Write(Guid value, StringBuilder target)
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