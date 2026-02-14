using System;
using System.Runtime.CompilerServices;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;

namespace MonoFSM.Core.DataProvider
{
    //Float來源：直接給一個float, variable float, data的property
    //不要亂放IValueProvider? 要不然provider會有很多種來源沒得選
    //最後才限定？可是要寫很多種？尷尬
    //有各式各樣的來源

    //MonoObject
    //MonoVariable來源
    //SOData來源
    public interface IFloatProvider : IValueProvider<float> //用介面好處，但是會哭哭
    {
        //監聽？
        // public bool IsDirty { get; }
    }

    public interface IFloatBoundProvider
    {
        float Min { get; }
        float Max { get; }
    }

    public interface IEntityValueProvider : IValueProvider<MonoEntity> //怎麼
    {
        MonoEntityTag entityTag { get; }
    }

    public interface IValueProvider<out T> : IValueProvider
    {
        [ShowInDebugMode]
        public T Value { get; }

        T1 IValueGetter.Get<T1>()
        {
            var value = Value;
            var t1Value = Unsafe.As<T, T1>(ref value);
            //FIXME: editor/debug mode 做type casting 檢查？
            // if (value is T1 t1Value) -> 會gc
            return t1Value;
            return default;
        }

        Type IValueGetter.ValueType => typeof(T);
        //FIXME: valuechange?
    }

    // [InlineProperty]
    // [Serializable]
    // public class FloatProviderLiteral : IFloatProvider
    // {
    //     public float literal;
    //
    //     public float GetFloat()
    //     {
    //         return literal;
    //     }
    //
    //     public string Description => literal.ToString();
    //
    //     public object GetValue()
    //     {
    //         return literal;
    //     }
    //
    //     public T GetValue<T>()
    //     {
    //         if (typeof(T) == typeof(float)) return (T)(object)literal;
    //
    //         throw new InvalidCastException($"Cannot cast {typeof(float)} to {typeof(T)}");
    //     }
    //
    //     public string GetDescription()
    //     {
    //         return literal.ToString();
    //     }
    // }

    // [MovedFrom(false, null, "rcg.rcgmakercore.Runtime", "FloatProviderFromVariable")]
    // [InlineProperty]
    // [Serializable]
    // public class VarFloatDropDownRef : IFloatProvider //FIXME: 改名成 DropdownVarFloat?
    // {
    //     [FormerlySerializedAs("_monoVariable")] [FormerlySerializedAs("_variable")] [HideLabel] [DropDownRef]
    //     public VarFloat _monoVar;
    //
    //     public float Value => _monoVar?.FinalValue ?? 0;
    //
    //
    //     public string Description => _monoVar?._varTag?.name;
    //
    //
    //     public T GetValue<T>()
    //     {
    //         if (typeof(T) == typeof(float)) return (T)(object)_monoVar?.FinalValue;
    //
    //         throw new InvalidCastException($"Cannot cast {typeof(float)} to {typeof(T)}");
    //     }
    //
    //     public Type ValueType => typeof(float);
    //
    //     public string GetDescription()
    //     {
    //         return _monoVar?.FinalValue.ToString();
    //     }
    //
    //     public bool IsDirty => _monoVar?.IsDirty ?? false;
    // }

    // [Serializable]
    // public class VariableBoolProvider : VariableProvider<VarBool, bool>
    // {
    //     public bool GetBool()
    //     {
    //         return Value;
    //     }
    //
    //     public string Description => varTag?.name;
    // }
    //
    // //平常都該用這個宣告？封裝過的VarFloat, 又有tag, 但沒有global instance
    // //serializeable class很噁心？
    // // [Obsolete]
    // //FIXME: 不該再用這種？非component的
    // [Serializable]
    // public class VariableFloatProvider : VariableProvider<VarFloat, float> //, IFloatProvider 反而不要用provider?
    // {
    //     //這個只管了value, 沒有管是什麼var...
    //     public float GetFloat()
    //     {
    //         return Value;
    //     }
    //
    //     public string Description => varTag?.name;
    //
    //     public VarFloat GetVar()
    //     {
    //         return GetVar<VarFloat>();
    //     }
    // }

    // [Serializable]
    // public class VariableIntProvider : VariableProvider<VarInt, int>, IFloatProvider
    // {
    //     public int GetInt()
    //     {
    //         return Value;
    //     }
    //
    //     public float GetFloat()
    //     {
    //         return Value;
    //     }
    //
    //     public string Description => varTag?.name;
    //
    //     public VarInt GetVar()
    //     {
    //         return GetVar<VarInt>();
    //     }
    // }
    //
    // [Serializable]
    // public class VariableFloatFromGlobalInstance : VariableProviderFromGlobalInstance<VarFloat>, IFloatProvider
    // {
    //     public float GetFloat()
    //     {
    //         return GetMonoVar().Value;
    //     }
    //
    //     public string Description => monoDescriptableTag.name + "." + varTag.name;
    // }
    //
    // [Serializable]
    // public class VarMonoFromGlobalInstance : VariableProviderFromGlobalInstance<VarMono>,
    //     IVarMonoProvider
    // {
    //     public string Description => monoDescriptableTag.name + "." + varTag.name;
    //     public VarMono Variable => GetMonoVar();
    //     public DescriptableData SampleData => Variable?.SampleData;
    // }
}
