using MonoFSM.Variable.FieldReference;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#endif

namespace MonoFSM.Variable
{
    //FIXME: 不可以繼承 GenericUnityObjectVariable
    [FormerlyNamedAs("VarDescriptableData")]
    public class VarGameData : GenericUnityObjectVariable<GameData>
    {
        protected override bool HasError() //額外寫validation好嗎?
        {
            if (base.HasError())
                return true;
            //ProxySource / runtime-only 時 _defaultValue 本來就會被隱藏跳過，不該再判紅
            if (!HideDefaultValue() && Value == null && _defaultValue == null)
            {
                _errorMessage = "Value 與 _defaultValue 皆為 null，需指定預設值";
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        //對目前的 _defaultValue 建一顆 variant（原 asset 當 base），並把 _defaultValue 指到新 variant
        [Button("從 _defaultValue 建立 Variant", ButtonSizes.Medium)]
        [HideIf(nameof(HideDefaultValue))]
        private void CreateVariantFromDefault()
        {
            if (_defaultValue == null)
            {
                Debug.LogError("[VarGameData] _defaultValue 沒設，無法建立 variant", this);
                return;
            }

            var variant = GameData.CreateVariantAsset(_defaultValue, GetOwnerPrefabName());
            if (variant == null)
                return;

            Undo.RecordObject(this, "Create GameData Variant");
            _defaultValue = variant;
            EditorUtility.SetDirty(this);
            //在 prefab instance 上按的話要讓 override 寫回去
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            EditorGUIUtility.PingObject(variant);
            Debug.Log($"[VarGameData] _defaultValue 已指向新 variant {variant.name}", this);
        }

        /// <summary>
        ///     新 variant 的檔名要用「這顆 VarGameData 所在的 prefab 名」。
        ///     三種情境依序試：Prefab 編輯模式看 stage 的 assetPath、scene 上的 prefab instance 看它的來源 asset、
        ///     都不是（純 scene 物件）就退回 hierarchy root 的名字。
        /// </summary>
        private string GetOwnerPrefabName()
        {
            //1. 正在 Prefab Stage 裡編輯：stage 的 assetPath 才是真正的 prefab，contentsRoot.name 可能被改過
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.IsPartOfPrefabContents(gameObject))
            {
                var stageName = System.IO.Path.GetFileNameWithoutExtension(stage.assetPath);
                if (!string.IsNullOrEmpty(stageName))
                    return stageName;
            }

            //2. scene 上的 prefab instance：取最近的 instance root 對應的 prefab asset
            var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            if (instanceRoot != null)
            {
                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
                if (!string.IsNullOrEmpty(assetPath))
                    return System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }

            //3. 純 scene 物件，只能用 root 名字
            return transform.root.name;
        }
#endif
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
