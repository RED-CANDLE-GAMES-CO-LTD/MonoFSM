using System;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    

    //FIXME: 玩惹九日有用到...
    public class VariableFloatVirtual : VariableFloat //這個是不是沒有屁用, 還是純屬拿來rebind?
    {
        //要標注等等才會有嗎？

        public VariableFloat variableFloat;

        public override float FinalValue => variableFloat ? variableFloat.CurrentValue : 0; //用接過來的變數

        // [PreviewInInspector] [Auto] private AbstractVariableModifier<float> modifier;

        // [ShowInPlayMode]
        // public float Value
        // {
        //     get => modifier.AfterGetValueModifyCheck(variableFloat.Value);
        //     set => variableFloat.Value = modifier.BeforeSetValueModifyCheck(value);
        // }
        // [Component(typeof(AbstractVariableModifier<float>))]
        // [PreviewInInspector] [Auto] private AbstractVariableModifier<float> modifier;
        
    }
}