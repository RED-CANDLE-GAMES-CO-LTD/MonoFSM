using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableFloatVirtual : MonoBehaviour, IVariableFloat
    {
        [Required] public VariableFloat variableFloat;
        [PreviewInInspector] [Auto] private AbstractVariableModifier<float> modifier;

        [ShowInPlayMode]
        public float Value
        {
            get => modifier.AfterGetValueModifyCheck(variableFloat.Value);
            set => variableFloat.Value = modifier.BeforeSetValueModifyCheck(value);
        }
    }
}