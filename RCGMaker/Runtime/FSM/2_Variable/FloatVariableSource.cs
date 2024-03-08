using System;
using RCGMaker.Core;
using Sirenix.OdinInspector;

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

    public interface IBoolValue
    {
        bool IsValid { get; }
    }

    [InlineProperty]
    [Serializable]
    public class BoolValueSource : InterfaceMonoRef<StateMachineOwner, IBoolValue>, IBoolValue
    {
        public bool IsValid => ((IBoolValue)ValueSource).IsValid;
    }
}