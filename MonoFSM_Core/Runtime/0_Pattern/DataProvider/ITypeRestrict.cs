using System;
using System.Collections.Generic;

namespace RCGMaker.Core.DataProvider
{
    public interface ITypeRestrict
    {
        public List<Type> SupportedTypes { get; }
    }
}