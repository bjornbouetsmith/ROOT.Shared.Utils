using System;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public static class SimpleFormatter<T>
    {
        private static ITypeFormatter<T> _instance;
        public static ITypeFormatter<T> Instance => _instance ?? (_instance = GetInstance());

        private static ITypeFormatter<T> GetInstance()
        {
            if (typeof(T).IsEnum)
            {
                var concrete = typeof(EnumFormatter<>).MakeGenericType(typeof(T));
                return (ITypeFormatter<T>)Activator.CreateInstance(concrete);
            }

            var jsonValueFormatter = new SimpleValueFormatter();
            return (ITypeFormatter<T>)jsonValueFormatter;
        }
    }

    public class SimpleFormatter : IFormatter
    {
        private bool _hasWrittenArrayValueSep;
        public void BeginObject(StringBuilder target)
        {
        }

        public void EndObject(StringBuilder target)
        {
        }

        public void BeginField(string fieldName, StringBuilder target)
        {
            target.Append(fieldName);
            target.Append(":");
        }

        public void EndField(string fieldName, StringBuilder target)
        {
            target.Append(",");
        }

        public void WriteValue<T>(T data, StringBuilder target)
        {
            SimpleFormatter<T>.Instance.Write(data, target);
        }

        public void BeginArray(StringBuilder target)
        {
            target.Append("[");
        }

        public void WriteArrayValueSep(StringBuilder target)
        {
            target.Append(",");
            _hasWrittenArrayValueSep = true;
        }

        public void EndArray(StringBuilder target)
        {
            if (_hasWrittenArrayValueSep)
            {
                _hasWrittenArrayValueSep = false;
                if (target.Length > 0)
                {
                    target.Length -= 1;
                }
            }

            target.Append("]");
        }

        public ITypeFormatter<T> Get<T>()
        {
            return SimpleFormatter<T>.Instance;
        }
    }
}