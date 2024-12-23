using System;
using RCGMaker.Core;
using RCGMaker.Runtime.FSM._2_Variable.VariableBinder;
using Sirenix.OdinInspector;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [InlineProperty]
    [Serializable]
    public class FloatValueSource : InterfaceMonoRef<StateMachineOwner, IFloatValue>, IFloatValue
    {
        public float FinalValue => ValueSource != null ? ((IFloatValue)ValueSource).FinalValue : ConstValue;
        [HideIf("@ValueSource != null")]
        public float ConstValue;
    }


    //FIXME: 為什麼要從condition下面拿？
    [InlineProperty]
    [Serializable]
    public class FloatValueRef : InterfaceMonoRef<AbstractConditionComp, IFloatValue>, IFloatValue
    {
        public float FinalValue => ((IFloatValue)ValueSource).FinalValue;
    }

    public interface IIntValue
    {
        int FinalValue { get; }
    }
    public interface ISerializedFloatValue
    {
        float EditorValue { get; set; }
    }
    public interface IFloatValue
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