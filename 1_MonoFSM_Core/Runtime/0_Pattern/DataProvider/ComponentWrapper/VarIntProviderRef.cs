using System;
using MonoFSM.Core.DataProvider;

namespace MonoFSM.VarRefOld
{
    [Obsolete]
    public class VarIntProviderRef : VariableProviderRef<VarInt, int>, IIntProvider
    {
        public string Description => varTag?.name;
        public int IntValue => Value;
    }
}