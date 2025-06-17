using MonoFSM.Core.DataProvider;

namespace MonoFSM.VarRef
{
    public class VarIntProviderRef : VariableProviderRef<VarInt, int>, IFloatProvider, IIntProvider
    {
        public float GetFloat()
        {
            return Value;
        }

        public string Description => varTag?.name;
        public int IntValue => Value;
    }
}