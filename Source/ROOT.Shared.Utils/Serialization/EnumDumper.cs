using System.Text;

namespace ROOT.Shared.Utils.Serialization
{
    public class EnumDumper<T> : TypeDumper<T>
    {
        public override string Dump(T value)
        {
            return value.ToString();
        }

        public override string Dump(T what, IFormatter formatter)
        {
            var sb = new StringBuilder();

            Dump(what, formatter, sb);

            return sb.ToString();
        }

        public override StringBuilder Dump(T what, IFormatter formatter, StringBuilder target)
        {
            formatter.WriteValue(what, target);

            return target;
        }

        public override string Dump(object what)
        {
            return Dump((T)what);
        }
    }
}