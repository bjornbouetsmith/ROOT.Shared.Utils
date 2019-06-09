using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public interface IFormatter
    {
        void BeginObject(StringBuilder target);
        void EndObject(StringBuilder target);
        void BeginField(string fieldName, StringBuilder target);
        void EndField(string fieldName, StringBuilder target);
        void WriteValue<T>(T data, StringBuilder target);

        void BeginArray(StringBuilder target);
        void EndArray(StringBuilder target);

        ITypeFormatter<T> Get<T>();
    }
}
