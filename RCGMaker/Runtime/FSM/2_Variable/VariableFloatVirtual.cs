using System;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [InlineProperty]
    [Serializable]
    public class FloatValueSource : InterfaceMonoRef<StateMachineOwner, IFloatValue>, IFloatValue
    {
        public float FinalValue => ((IFloatValue)ValueSource).FinalValue;
    }
    public interface IFloatValue
    {
        float FinalValue { get; }
    }

    public class VariableFloatVirtual : VariableFloat //這個是不是沒有屁用, 還是純屬拿來rebind?
    {
        //要標注等等才會有嗎？

        public VariableFloat variableFloat;

        public override float FinalValue => variableFloat ? variableFloat.Value : 0; //用接過來的變數

        [Component(typeof(AbstractVariableModifier<float>))]
        [PreviewInInspector] [Auto] private AbstractVariableModifier<float> modifier;
        
    }
}