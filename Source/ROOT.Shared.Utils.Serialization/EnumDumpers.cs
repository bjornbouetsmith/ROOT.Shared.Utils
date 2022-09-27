using System;

namespace ROOT.Shared.Utils.Serialization
{
    public static class EnumDumpers<T> where T : Enum
    {
        private static EnumDumper<T> _instance;
        public static EnumDumper<T> Instance => _instance ??= new EnumDumper<T>();
    }
}