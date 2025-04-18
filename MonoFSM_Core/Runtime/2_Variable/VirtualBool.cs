using MonoFSM.Variable;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    public class VirtualBool : VarBool
    {
        [FormerlySerializedAs("bindedVariable")]
        public VarBool _bindedMonoVariable;

        public override bool FinalValue => CurrentValue;
    }
}