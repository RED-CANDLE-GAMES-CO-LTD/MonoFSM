using UnityEngine;

namespace MonoFSM.Variable.VirutalizeVariable
{
    //FIXME: 這啥？
    public class EffectValue : MonoBehaviour, IFloatValueProvider
    {
        public VarFloat baseValue;
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