using RCGMaker.Core.DataProvider;

namespace RCGMakerFSM.RCGMakerFSMCore._0_Pattern.DataProvider.ComponentWrapper
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