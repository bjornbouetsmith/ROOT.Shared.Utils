using System;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class EnumFormatter<T> : ITypeFormatter<T>
        where T : Enum
    {
        public void Write(T value, StringBuilder target)
        {
            target.Append("\"");
            target.Append(value.ToString());
            target.Append("\"");
        }
    }
}