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

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var concrete = typeof(SimpleNullableFormatter<>).MakeGenericType(typeof(T).GetGenericArguments()[0]);
                return (ITypeFormatter<T>)Activator.CreateInstance(concrete);
            }

            var valueFormatter = new SimpleValueFormatter();
            return (ITypeFormatter<T>)valueFormatter;
        }
    }

    public class SimpleNullableFormatter<T> : ITypeFormatter<T?>
        where T : struct
    {
        public void Write(T? value, StringBuilder target)
        {
            if (value.HasValue)
            {
                SimpleFormatter<T>.Instance.Write(value.Value, target);
            }
        }
    }

    public class SimpleFormatter : IFormatter
    {
        private bool _hasWrittenArrayValueSep;
        private bool _hasWrittenFieldSep;
        private bool _hasWrittenBeginField;
        public void BeginObject(StringBuilder target)
        {
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
        }

        public void BeginField(string fieldName, StringBuilder target)
        {
            _hasWrittenBeginField = true;
            target.Append(fieldName);
            target.Append(":");
        }

        public void EndField(string fieldName, StringBuilder target)
        {
            if (_hasWrittenBeginField)
            {
                _hasWrittenBeginField = false;
                target.Append("|");
            }
        }

        public void WriteFieldSep(StringBuilder target)
        {
            _hasWrittenFieldSep = true;
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