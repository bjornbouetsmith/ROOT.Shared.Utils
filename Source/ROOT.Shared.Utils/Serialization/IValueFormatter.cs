﻿using System;

namespace ROOT.Shared.Utils.Serialization
{
    public interface IValueFormatter :
        ITypeFormatter<string>,
        ITypeFormatter<DateTime>,
        ITypeFormatter<char>,
        ITypeFormatter<byte>,
        ITypeFormatter<short>,
        ITypeFormatter<int>,
        ITypeFormatter<long>,
        ITypeFormatter<float>,
        ITypeFormatter<double>,
        ITypeFormatter<decimal>
    {
    }
}