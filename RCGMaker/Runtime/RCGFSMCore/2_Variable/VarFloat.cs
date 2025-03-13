using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;

// [Serializable]
// public class VariableFloatProvider : IFloatValueProvider
// {
//     public float FinalValue => _source.CurrentValue;
//     [DropDownRef] [SerializeField] VarFloat _source;
// }

//CountdownTimer...直接掛在這個下面？

public class VarFloat : GenericMonoVariable<ScriptableDataFloat, FlagFieldFloat, float>, IFloatValueProvider,
    IValueOfKey<VariableTag>, ISerializedFloatValue
{
    //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...? 
    public override GameFlagBase FinalData => ScriptableData;
    public VariableTag Key => varTag;
    public int IntValue => Mathf.CeilToInt(CurrentValue);
    public float Percentage => (CurrentValue - Min) / (Max - Min);
    public float Min => _boundModifier.MinValue;
    public float Max => _boundModifier.MaxValue;
    public bool IsMax => CurrentValue >= Max;

    [AutoChildren(false)] [PreviewInInspector]
    VariableFloatBoundModifier _boundModifier;

    // [PreviewInInspector] [Component] [AutoChildren]
    // AbstractVariableModifier<float>[] _setOperations;

    public float EditorValue
    {
        get => Field.ProductionValue;
        set
        {
            Field.ProductionValue = value;
            Field.DevValue = value;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    public float Value => CurrentValue;
}