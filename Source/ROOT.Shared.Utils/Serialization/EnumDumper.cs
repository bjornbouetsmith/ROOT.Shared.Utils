namespace ROOT.Shared.Utils.Serialization
{
    public class EnumDumper<T> : TypeDumper<T>
    {
        public override string Dump(T value)
        {
            return value.ToString();
        }

        public override string Dump(object what)
        {
            return Dump((T)what);
        }
    }
}