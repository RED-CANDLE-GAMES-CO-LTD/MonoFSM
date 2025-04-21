using System.Globalization;
using MonoFSM.Variable;
using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace MonoFSM.DataProvider
{
    /// <summary>
    /// Provide a reference to a VarFloat.
    /// </summary>
    public class VarFloatProviderRef : VariableProviderRef<VarFloat, float>, IFloatProvider
    {
        public float GetFloat()
        {
            return Value;
        }

        public string Description => varTag?.name;
    }
}