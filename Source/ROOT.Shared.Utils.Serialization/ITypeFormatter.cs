using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public interface ITypeFormatter<in T>
    {
        void Write(T value, StringBuilder target);
    }
}