using MonoFSM.Variable.FieldReference;

namespace MonoFSM.Variable
{
    //FIXME: 不可以繼承 GenericUnityObjectVariable
    [FormerlyNamedAs("VarDescriptableData")]
    public class VarGameData : GenericUnityObjectVariable<GameData>
    {
        protected override bool HasError() //額外寫validation好嗎?
        {
            return base.HasError() || (Value == null && _defaultValue == null);
        }
        // /// <summary>
        // /// 返回動態型別，讓反射系統能看到實際的子類別成員
        // /// </summary>
        // public override Type ValueType => GetDynamicValueType();
        //
        // /// <summary>
        // /// 取得動態數值型別，優先使用VarTag的RestrictType
        // /// </summary>
        // private Type GetDynamicValueType()
        // {
        //     if (_varTag?.ValueFilterType != null &&
        //         typeof(GameData).IsAssignableFrom(_varTag.ValueFilterType))
        //     {
        //         return _varTag.ValueFilterType; // 返回具體的子類別型別如FoodData
        //     }
        //
        //     return typeof(GameData); // 預設返回GameData
        // }
        //
        //
        // public new GameData Value
        // {
        //     get
        //     {
        //         var baseValue = base.Value;
        //         return CastToRestrictType(baseValue);
        //     }
        // }
        //
        // /// <summary>
        // /// 提供動態型別轉換，供_pathEntries等反射系統使用
        // /// </summary>
        // public object ValueData => Value;
        //
        // /// <summary>
        // /// 動態轉型幫助方法
        // /// </summary>
        // private GameData CastToRestrictType(GameData baseValue)
        // {
        //     if (baseValue == null || _varTag == null)
        //         return baseValue;
        //
        //     var restrictType = _varTag.ValueFilterType;
        //     if (restrictType != null &&
        //         typeof(GameData).IsAssignableFrom(restrictType) &&
        //         restrictType.IsInstanceOfType(baseValue))
        //     {
        //         return baseValue; // 已經是正確的子類別，直接返回
        //     }
        //
        //     return baseValue;
        // }
        //
        // /// <summary>
        // /// 強型別的泛型取值方法
        // /// </summary>
        // public new T GetValue<T>() where T : class
        // {
        //     var value = Value;
        //     if (value == null)
        //         return null;
        //
        //     if (value is T typedValue)
        //         return typedValue;
        //
        //     return base.GetValue<T>();
        // }
        //
        // /// <summary>
        // /// 型別安全的強制轉型方法，提供IntelliSense支援
        // /// </summary>
        // public T As<T>() where T : GameData
        // {
        //     var value = Value;
        //     if (value is T typedValue)
        //         return typedValue;
        //
        //     if (_varTag?.ValueFilterType == typeof(T))
        //     {
        //         return value as T;
        //     }
        //
        //     Debug.LogWarning($"無法將 {value?.GetType().Name} 轉型為 {typeof(T).Name}。請檢查VarTag設定。", this);
        //     return null;
        // }
        //
        // /// <summary>
        // /// 檢查當前值是否為特定型別
        // /// </summary>
        // public bool Is<T>() where T : GameData
        // {
        //     return Value is T;
        // }
        //
        // /// <summary>
        // /// 檢查VarTag是否限制為特定型別
        // /// </summary>
        // public bool IsRestrictedTo<T>() where T : GameData
        // {
        //     return _varTag?.ValueFilterType == typeof(T);
        // }

        // [ShowInInspector]
        // [SOConfig("10_Flags/GameData", useVarTagRestrictType: true)] //已經有了
        // private GameData CreateDefault
        // {
        //     set => _defaultValue = value;
        // }
    }
}
