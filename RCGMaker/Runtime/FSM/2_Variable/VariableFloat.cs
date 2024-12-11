
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;

public class VariableFloat : GenericVariable<ScriptableDataFloat, FlagFieldFloat, float>, IFloatValue, IValueOfKey<VariableTag>
{
    //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...? 
    public VariableTag Key => varTag;
    public int IntValue => Mathf.CeilToInt(CurrentValue);
    public float Percentage => (CurrentValue - Min) / (Max - Min);
    public float Min => _boundModifier.MinValue; 
    public float Max => _boundModifier.MaxValue;
    [Auto(false)] [PreviewInInspector] VariableFloatBoundModifier _boundModifier;
    [PreviewInInspector]
    [Component]
    [AutoChildren]
    AbstractVariableModifier<float> [] _setOperations;
    
}