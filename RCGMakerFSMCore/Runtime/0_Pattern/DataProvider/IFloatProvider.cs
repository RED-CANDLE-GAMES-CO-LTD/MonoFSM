using System;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace RCGMaker.Core.DataProvider
{
    //Float來源：直接給一個float, variable float, data的property

    //有各式各樣的來源

    //MonoObject
    //MonoVariable來源
    //SOData來源 
    public interface IFloatProvider //用介面好處，但是會哭哭
    {
        public float GetFloat();

        public float Value => GetFloat();

        string Description { get; }
        //string description name? provider description?
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

        public string Description => literal.ToString();
    }

    [MovedFrom(false, null, "rcg.rcgmakercore.Runtime", "FloatProviderFromVariable")]
    [InlineProperty]
    [Serializable]
    public class VarFloatDropDownRef : IFloatProvider //FIXME: 改名成 DropdownVarFloat?
    {
        [FormerlySerializedAs("_monoVariable")] [FormerlySerializedAs("_variable")] [HideLabel] [DropDownRef]
        public VarFloat _monoVar;

        public float GetFloat()
        {
            return _monoVar?.FinalValue ?? 0;
        }

        public string Description => _monoVar?._varTag?.name;
    }

    [Serializable]
    public class VariableBoolProvider : VariableProvider<VarBool, bool>
    {
        public bool GetBool()
        {
            return Value;
        }

        public string Description => varTag?.name;
    }

    //平常都該用這個宣告？封裝過的VarFloat, 又有tag, 但沒有global instance
    [Serializable]
    public class VariableFloatProvider : VariableProvider<VarFloat, float>//, IFloatProvider 反而不要用provider?
    {
        //這個只管了value, 沒有管是什麼var...
        public float GetFloat()
        {
            return Value;
        }

        public string Description => varTag?.name;

        public VarFloat GetVar()
        {
            return GetVar<VarFloat>();
        }
    }

    [Serializable]
    public class VariableIntProvider : VariableProvider<VarInt, int>, IFloatProvider
    {
        public int GetInt()
        {
            return Value;
        }

        public float GetFloat()
        {
            return Value;
        }

        public string Description => varTag?.name;

        public VarInt GetVar()
        {
            return GetVar<VarInt>();
        }
    }

    [Serializable]
    public class VariableFloatFromGlobalInstance : VariableProviderFromGlobalInstance<VarFloat>, IFloatProvider
    {
        public float GetFloat()
        {
            return GetMonoVar().Value;
        }

        public string Description => monoDescriptableTag.name + "." + varTag.name;
    }

    [Serializable]
    public class VarMonoFromGlobalInstance : VariableProviderFromGlobalInstance<VarMono>,
        IVarMonoProvider
    {
        public string Description => monoDescriptableTag.name + "." + varTag.name;
        public VarMono Variable => GetMonoVar();
        public DescriptableData SampleData => Variable?.SampleData;
    }
}