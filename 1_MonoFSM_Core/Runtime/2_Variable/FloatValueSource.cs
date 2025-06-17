using System;

using UnityEngine;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;

namespace MonoFSM.Variable
{
    //FIXME 用的到這個class嗎？
    // public class FloatValueSource : MonoBehaviour, IFloatProvider
    // {
    //     [SerializeReference] public IFloatProvider _valueSource;
    //
    //     public float GetFloat()
    //     {
    //         return _valueSource.GetFloat();
    //     }
    //
    //     public string Description => _valueSource.Description;
    // }
    // [InlineProperty]
    // [Serializable]
    // public class FloatValueSource : InterfaceMonoRef<StateMachineOwner, IFloatValueProvider>, IFloatValueProvider
    // {
    //     public float FinalValue => ValueSource != null ? ((IFloatValueProvider)ValueSource).FinalValue : ConstValue;
    //     [HideIf("@ValueSource != null")]
    //     public float ConstValue;
    // }


    //FIXME: 為什麼要從condition下面拿？
    // [InlineProperty]
    // [Serializable]
    // public class FloatValueRef : InterfaceMonoRef<AbstractConditionComp, IFloatValueProvider>, IFloatValueProvider
    // {
    //     public float FinalValue => ((IFloatValueProvider)ValueSource).FinalValue;
    // }

    public interface ISerializedFloatValue
    {
        float EditorValue { get; set; }
    }

    [Obsolete]
    public interface IFloatValueProvider
    {
        [PreviewInInspector] float FinalValue { get; }

        //要可以set?
    }

    public interface IBoolProvider
    {
        bool IsTrue { get; }
    }

    // [InlineProperty]
    // [Serializable]
    // public class BoolValueSource : InterfaceMonoRef<StateMachineOwner, IBoolValue>, IBoolValue
    // {
    //     //從StateMachineOwner下面找到所有的IBoolValue
    //     public bool IsTrue => ((IBoolValue)ValueSource).IsTrue; //FIXME: 如果ValueSource是null, 不好debug...
    // }
}