using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VirtualBool : VarBool
    {
        [FormerlySerializedAs("bindedVariable")]
        public VarBool _bindedMonoVariable;

        public override bool FinalValue => CurrentValue;
    }
}