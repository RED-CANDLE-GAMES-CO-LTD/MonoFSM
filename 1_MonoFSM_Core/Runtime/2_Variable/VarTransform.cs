using UnityEngine;

namespace MonoFSM.Variable
{
    public class VarTransform: GenericUnityObjectVariable<Transform>, ISettable<Transform>
    {
        // public override GameFlagBase FinalData { get; } //為什麼需要這個？
    }
}