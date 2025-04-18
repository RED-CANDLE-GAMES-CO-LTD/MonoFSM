using System;
using System.Collections.Generic;
using RCGExtension;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

//CountdownTimer...直接掛在這個下面？
namespace MonoFSM.Variable
{
    /// <summary>
    /// A MonoBehaviour representation of a float variable that can be bound to scriptable data.
    /// This class provides functionality for float values that can be accessed, modified, and tracked
    /// across the application.
    /// </summary>
    public class VarFloat : GenericMonoVariable<ScriptableDataFloat, FlagFieldFloat, float>,
        IValueOfKey<VariableTag>, ISerializedFloatValue
    {
        //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...? 
        public override GameFlagBase FinalData => BindData;
        // public VariableTag Key => _varTag;
        public int IntValue => Mathf.CeilToInt(CurrentValue);
        public float Percentage => (CurrentValue - Min) / (Max - Min);
        public float Min => _boundModifier.MinValue;
        public float Max => _boundModifier.MaxValue;
        public bool IsMax => CurrentValue >= Max;

        [AutoChildren(false)]
        [PreviewInInspector]
        private VariableFloatBoundModifier _boundModifier;
        // [PreviewInInspector] [Component] [AutoChildren]
        // AbstractVariableModifier<float>[] _setOperations;

        // public float Value => CurrentValue;
    }
}