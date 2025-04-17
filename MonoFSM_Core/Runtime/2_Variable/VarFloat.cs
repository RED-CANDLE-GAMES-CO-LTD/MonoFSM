using System;
using System.Collections.Generic;
using RCGExtension;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

// [Serializable]
// public class VariableFloatProvider : IFloatValueProvider
// {
//     public float FinalValue => _source.CurrentValue;
//     [DropDownRef] [SerializeField] VarFloat _source;
// }

//CountdownTimer...直接掛在這個下面？
namespace MonoFSM.Variable
{
    public class VarFloat : GenericMonoVariable<ScriptableDataFloat, FlagFieldFloat, float>,
        IValueOfKey<VariableTag>, ISerializedFloatValue
    {
        //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...? 
        public override GameFlagBase FinalData => BindData;
        public VariableTag Key => _varTag;
        public int IntValue => Mathf.CeilToInt(CurrentValue);
        public float Percentage => (CurrentValue - Min) / (Max - Min);
        public float Min => _boundModifier.MinValue;
        public float Max => _boundModifier.MaxValue;
        public bool IsMax => CurrentValue >= Max;

        [AutoChildren(false)] [PreviewInInspector]
        private VariableFloatBoundModifier _boundModifier;
        // [PreviewInInspector] [Component] [AutoChildren]
        // AbstractVariableModifier<float>[] _setOperations;

        // public float Value => CurrentValue;
    }
}