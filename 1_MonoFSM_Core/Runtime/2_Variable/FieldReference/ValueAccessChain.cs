using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// 表示一個值存取步驟的資料結構
    /// </summary>
    [Serializable]
    public class AccessStep
    {
        [HorizontalGroup("Step")] [HideLabel] [SerializeField] [SOConfig("FieldReference")]
        public FieldReference fieldReference;

        [HorizontalGroup("Step", Width = 100)] [ShowIf(nameof(IsArrayField))] [SerializeField]
        public int arrayIndex = 0;

        [PreviewInInspector]
        [ShowInInspector]
        [ReadOnly]
        public Type StepInputType => fieldReference?.SourceType;

        [PreviewInInspector]
        [ShowInInspector]
        [ReadOnly]
        public Type StepOutputType => fieldReference != null && fieldReference.IsArray && arrayIndex >= 0
            ? fieldReference.FieldType?.GetElementType()
            : fieldReference?.FieldType;

        public bool IsArrayField => fieldReference?.IsArray == true;

        /// <summary>
        /// 驗證此步驟是否有效
        /// </summary>
        public bool IsValid()
        {
            return fieldReference != null && fieldReference.ValidateReference();
        }

        /// <summary>
        /// 執行此存取步驟
        /// </summary>
        public object Execute(object input)
        {
            if (input == null || fieldReference == null)
                return null;

            var result = fieldReference.GetValue(input);

            // 如果是陣列且指定了索引
            if (fieldReference.IsArray && result is Array array)
            {
                if (arrayIndex < 0 || arrayIndex >= array.Length)
                {
                    Debug.LogError($"陣列索引 {arrayIndex} 超出範圍 (長度: {array.Length})");
                    return null;
                }

                return array.GetValue(arrayIndex);
            }

            return result;
        }
    }

    /// <summary>
    /// 值存取鏈 ScriptableObject，組合 VariableTag 和多個 FieldReference 形成完整的存取路徑
    /// </summary>
    [CreateAssetMenu(menuName = "RCG/Value Access Chain", fileName = "New Value Access Chain")]
    public class ValueAccessChain : ScriptableObject, IStringKey
    {
        [Header("變數來源")] [SerializeField] [OnValueChanged(nameof(OnVariableTagChanged))]
        private VariableTag _variableTag;

        [Header("存取步驟鏈")]
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, DraggableItems = true, ShowIndexLabels = true)]
        [OnValueChanged(nameof(OnAccessStepsChanged))]
        private List<AccessStep> _accessSteps = new();

        [Header("輸出型別資訊")] [SerializeField] [PreviewInInspector] [ReadOnly]
        private MySerializedType<object> _finalOutputType = new();

        [Header("說明")] [TextArea] public string Note;

        // Runtime 快取
        [NonSerialized] private string _cachedStringKey;

        [NonSerialized] private bool _isValidated = false;

        [NonSerialized] private string _lastValidationError;

        /// <summary>
        /// 變數標籤
        /// </summary>
        public VariableTag VariableTag => _variableTag;

        /// <summary>
        /// 存取步驟列表
        /// </summary>
        public IReadOnlyList<AccessStep> AccessSteps => _accessSteps.AsReadOnly();

        /// <summary>
        /// 最終輸出型別
        /// </summary>
        public Type FinalOutputType => _finalOutputType.RestrictType;

        /// <summary>
        /// 是否為有效的存取鏈
        /// </summary>
        public bool IsValid => _isValidated && string.IsNullOrEmpty(_lastValidationError);

        /// <summary>
        /// 最後的驗證錯誤訊息
        /// </summary>
        public string LastValidationError => _lastValidationError;

        public string GetStringKey
        {
            get
            {
                if (_cachedStringKey == null)
                {
                    var steps = string.Join(".", _accessSteps.Where(s => s.fieldReference != null)
                        .Select(s => s.fieldReference.FieldName));
                    _cachedStringKey = $"{name}_{_variableTag?.GetStringKey}_{steps}";
                }

                return _cachedStringKey;
            }
        }

        /// <summary>
        /// 當變數標籤改變時的回調
        /// </summary>
        private void OnVariableTagChanged()
        {
            _cachedStringKey = null;
            _isValidated = false;
            ValidateChain();
        }

        /// <summary>
        /// 當存取步驟改變時的回調
        /// </summary>
        private void OnAccessStepsChanged()
        {
            _cachedStringKey = null;
            _isValidated = false;
            ValidateChain();
        }

        /// <summary>
        /// 驗證整個存取鏈
        /// </summary>
        [Button("驗證存取鏈")]
        public bool ValidateChain()
        {
            _lastValidationError = "";
            _isValidated = false;

            // 使用新的型別安全驗證器
            var validationResult = TypeSafetyValidator.ValidateAccessChain(this);

            if (!validationResult.IsValid)
            {
                _lastValidationError = validationResult.ErrorMessage;
                return false;
            }

            // 設定最終輸出型別
            var finalType = _variableTag?.ValueType;
            if (_accessSteps.Count > 0)
            {
                var lastStep = _accessSteps[_accessSteps.Count - 1];
                finalType = lastStep.StepOutputType;
            }

            _finalOutputType.SetType(finalType);
            _isValidated = true;

            Debug.Log($"存取鏈驗證成功。最終型別: {finalType?.Name ?? "Unknown"}", this);
            return true;
        }

        /// <summary>
        /// 新增存取步驟
        /// </summary>
        [Button("新增步驟")]
        public void AddAccessStep()
        {
            _accessSteps.Add(new AccessStep());
            OnAccessStepsChanged();
        }

        /// <summary>
        /// 移除最後一個存取步驟
        /// </summary>
        [Button("移除最後步驟")]
        public void RemoveLastStep()
        {
            if (_accessSteps.Count > 0)
            {
                _accessSteps.RemoveAt(_accessSteps.Count - 1);
                OnAccessStepsChanged();
            }
        }

        /// <summary>
        /// 執行整個存取鏈，從變數開始到最終值
        /// </summary>
        public object ExecuteChain(object variableSource)
        {
            if (!IsValid)
            {
                Debug.LogError($"存取鏈無效: {_lastValidationError}", this);
                return null;
            }

            if (variableSource == null)
            {
                Debug.LogError("變數來源為 null", this);
                return null;
            }

            var currentValue = variableSource;

            // 如果沒有存取步驟，直接返回變數值
            if (_accessSteps.Count == 0) return currentValue;

            // 逐步執行每個存取步驟
            foreach (var step in _accessSteps)
            {
                currentValue = step.Execute(currentValue);
                if (currentValue == null)
                {
                    Debug.LogWarning("存取鏈中途遇到 null 值", this);
                    return null;
                }
            }

            return currentValue;
        }

        /// <summary>
        /// 執行存取鏈並轉換為指定型別
        /// </summary>
        public T ExecuteChain<T>(object variableSource)
        {
            var result = ExecuteChain(variableSource);

            if (result is T typedResult)
                return typedResult;

            if (result == null)
                return default;

            // 嘗試型別轉換
            try
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogError($"無法將結果轉換為 {typeof(T).Name}: {ex.Message}", this);
                return default;
            }
        }

        /// <summary>
        /// 取得存取鏈的字串表示（用於除錯）
        /// </summary>
        [Button("顯示存取路徑")]
        public string GetAccessPath()
        {
            if (_variableTag == null)
                return "未設定變數";

            var path = $"Var[{_variableTag.name}]";

            foreach (var step in _accessSteps)
                if (step.fieldReference != null)
                {
                    path += $".{step.fieldReference.FieldName}";
                    if (step.IsArrayField)
                        path += $"[{step.arrayIndex}]";
                }

            Debug.Log($"存取路徑: {path}", this);
            return path;
        }

        /// <summary>
        /// 重設存取鏈
        /// </summary>
        [Button("重設存取鏈")]
        public void ResetChain()
        {
            _variableTag = null;
            _accessSteps.Clear();
            _finalOutputType.SetType(null);
            _cachedStringKey = null;
            _isValidated = false;
            _lastValidationError = "";
        }

        /// <summary>
        /// 產生詳細的型別安全性報告
        /// </summary>
        [Button("產生型別安全性報告")]
        public void GenerateTypeSafetyReport()
        {
            var report = TypeSafetyValidator.GenerateTypeSafetyReport(this);
            Debug.Log(report, this);
        }

        /// <summary>
        /// 取得型別驗證結果的詳細清單
        /// </summary>
        public List<TypeValidationResult> GetDetailedValidationResults()
        {
            return TypeSafetyValidator.PerformFullAnalysis(this);
        }

        /// <summary>
        /// 檢查是否可以轉換為指定型別
        /// </summary>
        public bool CanConvertToType<T>()
        {
            return CanConvertToType(typeof(T));
        }

        /// <summary>
        /// 檢查是否可以轉換為指定型別
        /// </summary>
        public bool CanConvertToType(Type targetType)
        {
            if (!ValidateChain())
                return false;

            var finalOutputType = FinalOutputType;
            if (finalOutputType == null)
                return false;

            return TypeCompatibilityChecker.AreCompatible(finalOutputType, targetType);
        }

        /// <summary>
        /// 取得型別轉換建議
        /// </summary>
        public List<string> GetTypeConversionSuggestions<T>()
        {
            return GetTypeConversionSuggestions(typeof(T));
        }

        /// <summary>
        /// 取得型別轉換建議
        /// </summary>
        public List<string> GetTypeConversionSuggestions(Type targetType)
        {
            if (!ValidateChain())
                return new List<string> { "請先修正存取鏈的錯誤" };

            var finalOutputType = FinalOutputType;
            if (finalOutputType == null)
                return new List<string> { "無法確定最終輸出型別" };

            return TypeCompatibilityChecker.GetConversionSuggestions(finalOutputType, targetType);
        }

        /// <summary>
        /// 自動修復型別不匹配的問題（如果可能）
        /// </summary>
        [Button("自動修復")]
        public void AutoFix()
        {
            if (ValidateChain())
            {
                Debug.Log("存取鏈已經有效，無需修復", this);
                return;
            }

            var results = GetDetailedValidationResults();
            var errors = results.Where(r => !r.IsValid).ToList();

            if (errors.Count == 0)
            {
                Debug.Log("沒有發現錯誤，無需修復", this);
                return;
            }

            var hasFixed = false;

            // 嘗試修復空的欄位引用
            for (var i = _accessSteps.Count - 1; i >= 0; i--)
                if (_accessSteps[i].fieldReference == null)
                {
                    Debug.Log($"移除步驟 {i + 1}：欄位引用為空", this);
                    _accessSteps.RemoveAt(i);
                    hasFixed = true;
                }

            // 嘗試修復無效的陣列索引
            foreach (var step in _accessSteps)
                if (step.IsArrayField && step.arrayIndex < 0)
                {
                    Debug.Log("修正負數陣列索引為 0", this);
                    step.arrayIndex = 0;
                    hasFixed = true;
                }

            if (hasFixed)
            {
                Debug.Log("已嘗試自動修復，請重新驗證存取鏈", this);
                OnAccessStepsChanged();
            }
            else
            {
                Debug.Log("無法自動修復錯誤，請手動檢查設定", this);
                GenerateTypeSafetyReport();
            }
        }

        /// <summary>
        /// 同步所有欄位引用的名稱（用於 refactor 後的修復）
        /// </summary>
        [Button("同步所有欄位名稱")]
        public void SyncAllFieldNames()
        {
            Debug.Log("=== 開始同步所有欄位名稱 ===", this);

            var syncedCount = 0;
            var totalCount = 0;

            foreach (var step in _accessSteps)
                if (step.fieldReference != null)
                {
                    totalCount++;
                    var originalName = step.fieldReference.FieldName;

                    // 觸發驗證以進行自動同步
                    step.fieldReference.ValidateReference();

                    if (originalName != step.fieldReference.FieldName)
                    {
                        Debug.Log($"✓ 欄位名稱已同步：'{originalName}' -> '{step.fieldReference.FieldName}'", this);
                        syncedCount++;
                    }
                }

            if (syncedCount > 0)
            {
                Debug.Log($"同步完成：{syncedCount}/{totalCount} 個欄位名稱已更新", this);
                OnAccessStepsChanged(); // 重新驗證存取鏈
            }
            else
            {
                Debug.Log($"檢查完成：{totalCount} 個欄位名稱都已是最新的", this);
            }
        }

        /// <summary>
        /// 檢查所有欄位引用的同步狀態
        /// </summary>
        [Button("檢查欄位名稱同步狀態")]
        public void CheckAllFieldNamesSync()
        {
            Debug.Log("=== 檢查所有欄位名稱同步狀態 ===", this);

            var needSyncCount = 0;
            var totalCount = 0;

            foreach (var step in _accessSteps)
                if (step.fieldReference != null)
                {
                    totalCount++;
                    step.fieldReference.CheckFieldNameSync();
                    // 這裡可以擴展邏輯來統計需要同步的數量
                    // 由於 CheckFieldNameSync 只是輸出到 Console，我們暫時無法直接統計
                }

            Debug.Log($"檢查完成：已檢查 {totalCount} 個欄位引用", this);
            Debug.Log("如發現不同步的欄位，請使用 '同步所有欄位名稱' 按鈕進行修復", this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 編輯器預覽功能：顯示預期的存取路徑
        /// </summary>
        [ShowInInspector]
        [PreviewInInspector]
        [PropertyOrder(1000)]
        [MultiLineProperty(3)]
        [ReadOnly]
        private string PreviewAccessPath
        {
            get
            {
                if (_variableTag == null)
                    return "請先設定變數標籤";

                var preview = $"起始型別: {_variableTag.ValueType?.Name ?? "Unknown"}\n";
                preview += $"變數: {_variableTag.name}\n";

                var currentType = _variableTag.ValueType;

                for (var i = 0; i < _accessSteps.Count; i++)
                {
                    var step = _accessSteps[i];
                    if (step.fieldReference != null)
                    {
                        preview += $"步驟 {i + 1}: .{step.fieldReference.FieldName}";
                        if (step.IsArrayField)
                            preview += $"[{step.arrayIndex}]";
                        preview += $" -> {step.StepOutputType?.Name ?? "Unknown"}\n";
                        currentType = step.StepOutputType;
                    }
                    else
                    {
                        preview += $"步驟 {i + 1}: 未設定\n";
                    }
                }

                preview += $"最終型別: {currentType?.Name ?? "Unknown"}";

                if (!string.IsNullOrEmpty(_lastValidationError)) preview += $"\n錯誤: {_lastValidationError}";

                return preview;
            }
        }
#endif
    }
}