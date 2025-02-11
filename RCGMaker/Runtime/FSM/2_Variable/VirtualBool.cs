using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VirtualBool : VariableBool
    {
        [FormerlySerializedAs("bindedVariable")]
        public VariableBool _bindedMonoVariable;

        public override bool FinalValue => CurrentValue;
    }
}