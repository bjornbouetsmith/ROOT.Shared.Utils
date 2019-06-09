namespace ROOT.Shared.Utils.Serialization
{
    public static class ObjectDumper
    {
        public static string Dump(this object what)
        {
            return TypeDumper.Create(what.GetType()).Dump(what);
        }

        public static string Dump<T>(this T what)
        {
            return TypeDumper.Create<T>().Dump(what);
        }

        public static string Dump<T>(this T what, IFormatter formatter)
        {
            return TypeDumper.Create<T>().Dump(what, formatter);
        }
    }
}
