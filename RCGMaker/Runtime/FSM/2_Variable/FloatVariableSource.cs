using System;
using RCGMaker.Core;
using RCGMaker.Runtime.FSM._2_Variable.VariableBinder;
using Sirenix.OdinInspector;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [InlineProperty]
    [Serializable]
    public class FloatValueSource : InterfaceMonoRef<StateMachineOwner, IFloatValueProvider>, IFloatValueProvider
    {
        public float FinalValue => ValueSource != null ? ((IFloatValueProvider)ValueSource).FinalValue : ConstValue;
        [HideIf("@ValueSource != null")]
        public float ConstValue;
    }


    //FIXME: 為什麼要從condition下面拿？
    [InlineProperty]
    [Serializable]
    public class FloatValueRef : InterfaceMonoRef<AbstractConditionComp, IFloatValueProvider>, IFloatValueProvider
    {
        public float FinalValue => ((IFloatValueProvider)ValueSource).FinalValue;
    }
    
    public interface ISerializedFloatValue
    {
        float EditorValue { get; set; }
    }
    public interface IFloatValueProvider
    {
        float FinalValue { get; }
       
        //要可以set?
    }

    public interface IBoolValue
    {
        bool IsTrue { get; }
    }

    [InlineProperty]
    [Serializable]
    public class BoolValueSource : InterfaceMonoRef<StateMachineOwner, IBoolValue>, IBoolValue
    {
        //從StateMachineOwner下面找到所有的IBoolValue
        public bool IsTrue => ((IBoolValue)ValueSource).IsTrue; //FIXME: 如果ValueSource是null, 不好debug...
    }
}