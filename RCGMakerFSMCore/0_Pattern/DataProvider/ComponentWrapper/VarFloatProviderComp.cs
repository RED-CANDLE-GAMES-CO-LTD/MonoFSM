using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGFSMCore._0_Pattern.DataProvider.ComponentWrapper
{
    //VarFloatRef?
    public class VarFloatProviderComp : VariableProviderComp<VarFloat, float>, IFloatProvider
    {
        public float GetFloat()
        {
            return Value;
        }

        public string Description => varTag?.name;
    }
}