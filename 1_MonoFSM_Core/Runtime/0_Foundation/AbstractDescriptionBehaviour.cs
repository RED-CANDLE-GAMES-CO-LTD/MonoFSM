using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Text;
using MonoDebugSetting;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MonoFSM.Foundation
{
    // [InfoBox("$_errorMessage", InfoMessageType.Error, "$HasError")]
    [Searchable]
    public abstract class AbstractDescriptionBehaviour
        : MonoBehaviour,
            IBeforePrefabSaveCallbackReceiver,
            IAfterPrefabStageOpenCallbackReceiver,
            IDrawHierarchyBackGround
    {
        [HideIf(nameof(_parentObj))]
        [ShowIn(PrefabKind.PrefabInstance)]
        [RequiredIn(PrefabKind.PrefabInstance)]
        // [ShowInInspector]
        [AutoParent]
        protected MonoObj _parentObj; //FIXME: 會拿不到root的耶...好麻煩啊

        [ShowInDebugMode] [AutoParent(getMadIfMissing: false)]
        MonoObj[] _parentObjs; //用array避免拿不到parentobj

        public string PathName => _parentObjs != null && _parentObjs.Length > 0
            ? $"{ReformatedName} (" + string.Join(
                ".",
                Array.ConvertAll(_parentObjs, obj => obj.gameObject.name).Reverse()
            ) + ")"
            : _parentObj != null
                ? _parentObj.gameObject.name + "." + ReformatedName
                : ReformatedName;

        public void DespawnParentObj()
        {
            simulator.Despawn(_parentObj);
        }
        public WorldUpdateSimulator simulator => _parentObj.WorldUpdateSimulator;

#if UNITY_EDITOR
        [TextArea]
        [SerializeField]
        protected string _note; //這個應該要有另外的地方可以draw? 多component還會打架...
#endif

        /// <summary>
        ///     想要自己命名，又要帶有description tag的話就把Description改用這個
        /// </summary>
        protected string ReformatedName => FormatName(name);

        public string FormatName(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "";
            return Regex.Replace(str, @"\[.*?\]", "").Trim();
        }

        // Cache for required fields per type (serialized only)
        private static readonly Dictionary<Type, FieldInfo[]> _requiredFieldsCache = new();

        // Cache for required fields per type (including non-serialized)
        private static readonly Dictionary<
            Type,
            FieldInfo[]
        > _requiredFieldsCacheWithNonSerialized = new();

        // Track if we're in prefab stage mode for more detailed error checking
        // private bool _isPrefabStageMode = false;

        //沒有做AutoComponent下會顯示error? 還是應該讓prefab openstage時做一次，scene上跳過這個判定，雖然稍嫌trivial
        private static FieldInfo[] GetRequiredHierarchyValidateFields(
            Type type,
            bool includeNonSerialized = false
        )
        {
            var cache = includeNonSerialized
                ? _requiredFieldsCacheWithNonSerialized
                : _requiredFieldsCache;

            if (cache.TryGetValue(type, out var cachedFields))
                return cachedFields;

            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            // Find all fields with [Required] or [DropDownRef] attributes that are not "interfaces"
            //interface在組合component就會看到了, 也比較不會在refactor之後掉reference
            FieldInfo[] requiredFields;

            if (includeNonSerialized)
                // Include all fields with required attributes, regardless of serialization
                requiredFields = Array.FindAll(
                    fields,
                    f =>
                        (
                            f.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0
                            || f.GetCustomAttributes(typeof(DropDownRefAttribute), false).Length > 0
                        ) && !f.FieldType.IsInterface
                );
            else
                // Only include public or SerializeField attributes (original behavior)
                requiredFields = Array.FindAll(
                    fields,
                    f =>
                        (
                            f.GetCustomAttribute(typeof(RequiredAttribute), false) != null
                            || f.GetCustomAttribute(typeof(DropDownRefAttribute), false) != null
                        )
                        && !f.FieldType.IsInterface
                        && (
                            f.IsPublic
                            || f.GetCustomAttribute(typeof(SerializeField), false) != null
                        )
                );

            cache[type] = requiredFields;

            return requiredFields;
        }

        /// <summary>
        /// 欄位驗證結果
        /// </summary>
        private struct FieldValidationResult
        {
            public bool ShouldValidate;
            public string SkipReason; // 如果跳過，顯示原因（如 "HideIf:_useCustom=True"）
        }

        /// <summary>
        /// 檢查必填欄位是否為 null
        /// </summary>
        /// <param name="isPrefabStage">是否在 Prefab Stage 中檢查</param>
        /// <param name="isShowError">是否顯示錯誤訊息（僅在 Prefab Stage 時有效）</param>
        private bool CheckNullOfRequiredFields(bool isPrefabStage = false, bool isShowError = false)
        {
            var requiredFields = GetRequiredHierarchyValidateFields(GetType());
            var skippedFieldsInfo = new List<string>(); // 包含原因的跳過欄位資訊
            var validatedFields = new List<string>();
            var context = isPrefabStage ? "prefab" : "scene";

            foreach (var field in requiredFields)
            {
                // 檢查是否應該驗證此欄位（根據條件方法）
                var validationResult = GetFieldValidationResult(field);
                if (!validationResult.ShouldValidate)
                {
                    skippedFieldsInfo.Add($"{field.Name}({validationResult.SkipReason})");
                    continue;
                }

                var value = field.GetValue(this) as Object;
                if (value == null)
                {
                    var validatedInfo = validatedFields.Count > 0
                        ? $" (已驗證: {string.Join(", ", validatedFields)})"
                        : "";
                    var skippedInfo = skippedFieldsInfo.Count > 0
                        ? $" (跳過: {string.Join(", ", skippedFieldsInfo)})"
                        : "";
                    _errorMessage =
                        $"Required field '{field.Name}' is null in {gameObject.name}{validatedInfo}{skippedInfo}";

                    if (isPrefabStage && isShowError)
                    {
                        Debug.LogError(
                            $"Required field '{field.Name}' is null in {gameObject.name}",
                            this
                        );
#if UNITY_EDITOR
                        EditorGUIUtility.PingObject(this);
#endif
                    }

                    return true;
                }

                validatedFields.Add(field.Name);
            }

            var validatedInfoFinal = validatedFields.Count > 0
                ? $" (已驗證: {string.Join(", ", validatedFields)})"
                : "";
            var skippedInfoFinal = skippedFieldsInfo.Count > 0
                ? $" (跳過: {string.Join(", ", skippedFieldsInfo)})"
                : "";
            _errorMessage = $"pass in {context}!{validatedInfoFinal}{skippedInfoFinal}";
            return false;
        }

        /// <summary>
        /// 取得欄位驗證結果，包含是否應該驗證以及跳過原因
        /// </summary>
        private FieldValidationResult GetFieldValidationResult(FieldInfo field)
        {
            // 優先檢查 ShowIf/HideIf 屬性（自動同步 UI 顯示和驗證邏輯）
            var (shouldValidate, skipReason) = EvaluateShowIfHideIfConditionWithReason(field);
            if (skipReason != null)
                return new FieldValidationResult
                    { ShouldValidate = shouldValidate, SkipReason = skipReason };

            // 如果沒有 ShowIf/HideIf，則檢查 ValueTypeValidateAttribute 的 ConditionalMethod
            var validateAttribute = field.GetCustomAttribute<ValueTypeValidateAttribute>();
            if (validateAttribute == null ||
                string.IsNullOrEmpty(validateAttribute.ConditionalMethod))
                return new FieldValidationResult { ShouldValidate = true, SkipReason = null };

            var methodResult =
                EvaluateBoolConditionWithReason(validateAttribute.ConditionalMethod, field.Name);
            return new FieldValidationResult
            {
                ShouldValidate = methodResult.shouldValidate,
                SkipReason = methodResult.skipReason
            };
        }

        /// <summary>
        /// 評估 ShowIf/HideIf 屬性的條件，並回傳跳過原因
        /// </summary>
        /// <returns>(shouldValidate, skipReason) - skipReason 為 null 表示沒有 ShowIf/HideIf 屬性</returns>
        private (bool shouldValidate, string skipReason) EvaluateShowIfHideIfConditionWithReason(
            FieldInfo field)
        {
            // 檢查 ShowIfAttribute
            var showIfAttr = field.GetCustomAttribute<ShowIfAttribute>();
            if (showIfAttr != null)
            {
                var conditionName = showIfAttr.Condition;
                if (!string.IsNullOrEmpty(conditionName))
                {
                    var (result, conditionValue) =
                        EvaluateMemberConditionWithValue(conditionName, showIfAttr.Value);
                    if (result.HasValue)
                    {
                        if (!result.Value) // ShowIf 條件為 false，跳過驗證
                            return (false, $"ShowIf:{conditionName}={conditionValue}");
                        return (true, null); // 條件為 true，應該驗證
                    }
                }
            }

            // 檢查 HideIfAttribute（邏輯反轉）
            var hideIfAttr = field.GetCustomAttribute<HideIfAttribute>();
            if (hideIfAttr != null)
            {
                var conditionName = hideIfAttr.Condition;
                var (result, conditionValue) =
                    EvaluateMemberConditionWithValue(conditionName, hideIfAttr.Value);

                if (result.HasValue)
                {
                    if (result.Value) // HideIf 條件為 true，跳過驗證
                        return (false, $"HideIf:{conditionName}={conditionValue}");
                    return (true, null); // 條件為 false，應該驗證
                }
                else
                {
                    // 無法評估條件，預設跳過
                    return (false, $"HideIf:{conditionName}=無法評估");
                }
            }

            return (true, null); // 沒有 ShowIf/HideIf 屬性，應該驗證
        }

        /// <summary>
        /// 評估 bool 條件方法，並回傳跳過原因
        /// </summary>
        private (bool shouldValidate, string skipReason) EvaluateBoolConditionWithReason(
            string methodName, string fieldName)
        {
            try
            {
                var method = GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                if (method == null)
                {
                    Debug.LogWarning(
                        $"找不到條件方法 '{methodName}' 在型別 {GetType().Name} 中，欄位 '{fieldName}'");
                    return (true, null);
                }

                if (method.ReturnType != typeof(bool))
                {
                    Debug.LogWarning($"條件方法 '{methodName}' 必須返回 bool 型別，欄位 '{fieldName}'");
                    return (true, null);
                }

                var result = method.Invoke(this, null);
                var boolResult = result is bool b && b;
                if (!boolResult)
                    return (false, $"Condition:{methodName}()={boolResult}");
                return (true, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"執行條件方法 '{methodName}' 時發生錯誤: {ex.Message}，欄位 '{fieldName}'");
                return (true, null);
            }
        }

        /// <summary>
        /// 評估成員條件（欄位、屬性或方法），並回傳條件的實際值
        /// </summary>
        /// <param name="memberName">成員名稱</param>
        /// <param name="compareValue">比較值（用於 enum 比較，可為 null）</param>
        /// <returns>(result, actualValue) - result 為條件結果，actualValue 為成員的實際值字串</returns>
        private (bool? result, string actualValue) EvaluateMemberConditionWithValue(
            string memberName, object compareValue)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                // 遞迴搜尋繼承鏈（包含 base class 的 private/protected 成員）
                var currentType = GetType();
                while (currentType != null)
                {
                    // 1. 嘗試作為欄位
                    var fieldInfo = currentType.GetField(memberName, bindingFlags | BindingFlags.DeclaredOnly);
                    if (fieldInfo != null)
                    {
                        var value = fieldInfo.GetValue(this);
                        var result = EvaluateConditionValue(value, compareValue);
                        return (result, value?.ToString() ?? "null");
                    }

                    // 2. 嘗試作為屬性
                    var propertyInfo = currentType.GetProperty(memberName, bindingFlags | BindingFlags.DeclaredOnly);
                    if (propertyInfo != null && propertyInfo.CanRead)
                    {
                        var value = propertyInfo.GetValue(this);
                        var result = EvaluateConditionValue(value, compareValue);
                        return (result, value?.ToString() ?? "null");
                    }

                    // 3. 嘗試作為方法（無參數，返回 bool）
                    var methodInfo = currentType.GetMethod(memberName, bindingFlags | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                    if (methodInfo != null && methodInfo.ReturnType == typeof(bool))
                    {
                        var result = methodInfo.Invoke(this, null);
                        var boolResult = result is true;
                        return (boolResult, boolResult.ToString());
                    }

                    currentType = currentType.BaseType;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"評估條件 '{memberName}' 時發生錯誤: {ex.Message}");
            }

            return (null, "找不到成員"); // 找不到成員
        }

        /// <summary>
        /// 評估條件值
        /// </summary>
        private bool EvaluateConditionValue(object value, object compareValue)
        {
            // 如果有比較值，則進行相等比較（用於 enum）
            if (compareValue != null)
                return Equals(value, compareValue);

            // 否則嘗試轉換為 bool
            if (value is bool boolValue)
                return boolValue;

            // 非 bool 值且沒有比較值，無法評估
            return true; // 預設為驗證
        }

        [ShowInDebugMode]
        [AutoParent]
        protected MonoEntity _self; //FIXME: 每個都要嗎？

        public MonoEntity BindEntity
        {
            get
            {
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_self));
                // this.EnsureComponentInParent(ref _self);
                return _self;
            }
        }

        //介面上也顯示？textarea?
        public virtual string Description => GetType().Name;

        protected virtual string DescriptionTag => "";


        protected virtual bool IsIgnoreRename => false;

        [InfoBox("$Description")]
        [HideInInlineEditors]
        [Button]
        protected virtual void Rename()
        {
#if UNITY_EDITOR
            if (IsIgnoreRename)
                return;
            try
            {
                // Debug.Log("DescriptionTag: " + DescriptionTag, this);
                // Debug.Log(
                //     $"Description of {GetType()}: Description:{Description} process:{DescriptionPreprocess(Description)}",
                //     this);
                if (DescriptionTag == "")
                {
                    gameObject.name = $"{Description}";
                }
                else if (IsBracketsNeededForTag)
                    gameObject.name = $"[{DescriptionTag}] {Description}";
                else
                    gameObject.name = $"{DescriptionTag} {Description}";
                EditorUtility.SetDirty(gameObject);
                RevertNameOverrideIfMatchesPrefab();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Error renaming gameObject: {gameObject.name} to [{DescriptionTag}]",
                    this
                );
            }

#endif
        }

        protected static void RevertNameOverrideIfMatchesPrefab(GameObject go)
        {
#if UNITY_EDITOR
            if (!PrefabUtility.IsPartOfPrefabInstance(go)) return;

            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (source == null || go.name != source.name) return;

            var so = new SerializedObject(go);
            var nameProp = so.FindProperty("m_Name");
            PrefabUtility.RevertPropertyOverride(nameProp, InteractionMode.AutomatedAction);
#endif
        }

        void RevertNameOverrideIfMatchesPrefab()
        {
            RevertNameOverrideIfMatchesPrefab(gameObject);
        }

        protected virtual bool IsBracketsNeededForTag => true;

        protected virtual void Awake()
        {
        } //FIXME: 不該用這個？

        protected virtual void Start()
        {
        } //FIXME: 不該用這個？

        [Button("Save Process")]
        public virtual void OnBeforePrefabSave()
        {
#if UNITY_EDITOR
            AutoAttributeManager.AutoReference(this); //有些field需要autoChildren容易造成 description null
            CheckNullOfRequiredFields(isPrefabStage: true, isShowError: true);
            Rename();
#endif
        }

        public virtual void OnAfterPrefabStageOpen()
        {
            // _isPrefabStageMode = true;
            // Trigger error checking with non-serialized fields included
            CheckNullOfRequiredFields(isPrefabStage: true, isShowError: true);
        }

        // [InfoBox("$_errorMessage", InfoMessageType.Error, "$HasError")]
        protected virtual bool HasError()
        {
            // Use different checking logic based on environment
            bool isInPrefabStage = false;

#if UNITY_EDITOR
            // Double-check using Unity's API for more reliability
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                isInPrefabStage = true;
            // Debug.Log(
            //     $"HasError Check in {gameObject.name}, isInPrefabStage: {isInPrefabStage}",
            //     this
            // );
            return CheckNullOfRequiredFields(isPrefabStage: isInPrefabStage);
#else
            return false;
#endif
        }

        [InfoBox("$_errorMessage", InfoMessageType.Error, "$HasError")]
        [PreviewInDebugMode]
        protected string _errorMessage;

        public Color BackgroundColor => new(1.0f, 0f, 0f, 0.3f);

        [ShowInDebugMode]
        public bool IsDrawGUIHierarchyBackground => !Application.isPlaying && HasError(); //還是用icon?
        //FIXME: 動態做這個，bool IsNeedValid的Required? (配合啥？

        #region DebugMode Log (Zero GC)

        /// <summary>
        /// Debug 模式專用 Log，只在 RuntimeDebugSetting.IsDebugMode 時輸出，使用 ZString 避免 GC
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLog(string message)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            Debug.Log(message, this);
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLog<T1>(T1 arg1)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            using var sb = ZString.CreateStringBuilder();
            sb.Append(arg1);
            Debug.Log(sb.ToString(), this);
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLog<T1, T2>(T1 arg1, T2 arg2)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            var message = ZString.Concat(arg1, " ", arg2);
            Debug.Log(message, this);
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLog<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            var message = ZString.Concat(arg1, " ", arg2, " ", arg3);
            Debug.Log(message, this);
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLog<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            var message = ZString.Concat(arg1, " ", arg2, " ", arg3, " ", arg4);
            Debug.Log(message, this);
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLog<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            var message = ZString.Concat(arg1, " ", arg2, " ", arg3, " ", arg4, " ", arg5);
            Debug.Log(message, this);
        }

        /// <summary>
        /// Debug 模式專用 LogWarning
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLogWarning<T1>(T1 arg1)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            using var sb = ZString.CreateStringBuilder();
            sb.Append(arg1);
            Debug.LogWarning(sb.ToString(), this);
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLogWarning<T1, T2>(T1 arg1, T2 arg2)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            var message = ZString.Concat(arg1, " ", arg2);
            Debug.LogWarning(message, this);
        }

        /// <summary>
        /// Debug 模式專用 LogError
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLogError<T1>(T1 arg1)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            using var sb = ZString.CreateStringBuilder();
            sb.Append(arg1);
            Debug.LogError(sb.ToString(), this);
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        protected void DebugLogError<T1, T2>(T1 arg1, T2 arg2)
        {
            if (!RuntimeDebugSetting.IsDebugMode) return;
            var message = ZString.Concat(arg1, " ", arg2);
            Debug.LogError(message, this);
        }

        #endregion
    }
}
