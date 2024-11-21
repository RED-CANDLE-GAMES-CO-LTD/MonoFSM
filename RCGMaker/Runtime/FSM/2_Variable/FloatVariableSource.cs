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
        public float FinalValue => ((IFloatValue)ValueSource).FinalValue;
    }


    //FIXME: 為什麼要從condition下面拿？
    [InlineProperty]
    [Serializable]
    public class FloatValueRef : InterfaceMonoRef<AbstractConditionComp, IFloatValue>, IFloatValue
    {
        public float FinalValue => ((IFloatValue)ValueSource).FinalValue;
    }

    public interface IFloatValue
    {
        float FinalValue { get; }
    }

    public interface IBoolValue
    {
        bool IsValid { get; }
    }

    [InlineProperty]
    [Serializable]
    public class BoolValueSource : InterfaceMonoRef<StateMachineOwner, IBoolValue>, IBoolValue
    {
        //從StateMachineOwner下面找到所有的IBoolValue
        public bool IsValid => ((IBoolValue)ValueSource).IsValid;
    }
}