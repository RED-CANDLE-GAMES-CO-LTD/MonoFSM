using System;
using System.Collections.Generic;

namespace MonoFSM.Core.DataProvider
{
    public interface ITypeRestrict
    {
        public List<Type> SupportedTypes { get; }
    }
}