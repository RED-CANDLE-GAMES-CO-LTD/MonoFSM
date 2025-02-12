using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace RCGMaker.Core.DataProvider
{
    //Float來源：直接給一個float, variable float, data的property

    //有各式各樣的來源

    //MonoObject
    //MonoVariable來源
    //SOData來源 
    public interface IFloatProvider
    {
        public float GetFloat();
    }

    [InlineProperty]
    [Serializable]
    public class FloatProviderLiteral : IFloatProvider
    {
        public float literal;

        public float GetFloat()
        {
            return literal;
        }
    }

    [InlineProperty]
    [Serializable]
    public class FloatProviderFromVariable : IFloatProvider
    {
        [FormerlySerializedAs("_variable")] [HideLabel] [DropDownRef]
        public VariableFloat _monoVariable;

        public float GetFloat()
        {
            return _monoVariable.FinalValue;
        }
    }
}