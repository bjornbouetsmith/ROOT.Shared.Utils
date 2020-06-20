using System;
using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public static class JsonFormatter<T>
    {
        private static ITypeFormatter<T> _instance;
        public static ITypeFormatter<T> Instance => _instance ??= GetInstance();

        private static ITypeFormatter<T> GetInstance()
        {
            if (typeof(T).IsEnum)
            {
                var concrete = typeof(EnumFormatter<>).MakeGenericType(typeof(T));
                return (ITypeFormatter<T>)Activator.CreateInstance(concrete);
            }

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var concrete = typeof(JsonNullableFormatter<>).MakeGenericType(typeof(T).GetGenericArguments()[0]);
                return (ITypeFormatter<T>)Activator.CreateInstance(concrete);
            }

            var jsonValueFormatter = new JsonValueFormatter();
            return (ITypeFormatter<T>)jsonValueFormatter;
        }
    }

    public class JsonFormatter : IFormatter
    {
        private bool _hasWrittenArrayValueSep;
        private bool _hasWrittenFieldSep;

        public void BeginObject(StringBuilder target)
        {
            target.Append("{");
        }

        public void EndObject(StringBuilder target)
        {
            if (_hasWrittenFieldSep)
            {
                _hasWrittenFieldSep = false;
                if (target.Length > 0)
                {
                    target.Length -= 1;
                }
            }

            target.Append("}");
        }

        public void BeginField(string fieldName, StringBuilder target)
        {
            target.Append("\"");
            target.Append(fieldName);
            target.Append("\":");
        }

        public void EndField(string fieldName, StringBuilder target)
        {
            //target.Append(",");
        }

        public void WriteFieldSep(StringBuilder target)
        {
            _hasWrittenFieldSep = true;
            target.Append(",");
        }

        public void WriteValue<T>(T data, StringBuilder target)
        {
            JsonFormatter<T>.Instance.Write(data, target);

        }

        public void BeginArray(StringBuilder target)
        {
            target.Append("[");
        }

        public void WriteArrayValueSep(StringBuilder target)
        {
            _hasWrittenArrayValueSep = true;
            target.Append(",");
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
            return JsonFormatter<T>.Instance;
        }
    }
}