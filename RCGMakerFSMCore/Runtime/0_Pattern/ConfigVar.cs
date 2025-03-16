using System;
using RCGMaker.Runtime.FSM._2_Variable;

namespace RCGMaker.Core
{
    public interface IConfigVar
    {
        object GetValue();
    }

    //
    // [Serializable]
    // public class FloatConfig : IConfigVar, IFloatValueProvider
    // {
    //     public float value;
    //
    //     object IConfigVar.GetValue()
    //     {
    //         return value;
    //     }
    //
    //     public float FinalValue => value;
    // }
    //
    //
    // [Serializable]
    // public class IntConfig : IConfigVar, IIntProvider
    // {
    //     public int value;
    //
    //     object IConfigVar.GetValue()
    //     {
    //         return value;
    //     }
    //
    //     // public int FinalValue => value;
    //     public int IntValue => value;
    // }
}