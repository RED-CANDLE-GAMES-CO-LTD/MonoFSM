using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.VirutalizeVariable
{
    public class EffectValue : MonoBehaviour, IFloatValue
    {
        public VariableFloat baseValue;
        [AutoChildren] private IVariableFloatOperation[] modifiers;

        public float FinalValue
        {
            get
            {
                var value = baseValue.FinalValue;
                foreach (var modifier in modifiers)
                {
                    value = modifier.ApplyOperation(value);
                }

                return value;
            }
        }
    }
}