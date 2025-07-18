using System;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// 實作鏈式值提供者，使用 ValueAccessChain 來執行複雜的值存取邏輯
    /// 支援從 Variable 開始，透過多層欄位存取到最終值
    /// </summary>
    public class ChainedValueProvider : MonoBehaviour, IValueProvider, IVariableProvider
    {
        [SOConfig("Test/ChainedValueProviderConfig")]
        [Header("存取鏈配置")]
        [SerializeField]
        [Required]
        [OnValueChanged(nameof(OnAccessChainChanged))]
        private ValueAccessChain _accessChain;

        [Header("變數提供者")] [SerializeField] [Required] [CompRef] [Auto]
        private AbstractVariableProviderRef _variableProvider;

        [Header("快取設定")] [SerializeField] [Tooltip("是否啟用值快取以提升效能")]
        private bool _enableValueCaching = true;

        [SerializeField] [Tooltip("快取多長時間（秒），0 表示每幀都重新計算")]
        private float _cacheValidDuration = 0.1f;

        [Header("除錯資訊")] [SerializeField] [PreviewInInspector] [ReadOnly]
        private bool _isChainValid;

        [SerializeField] [PreviewInInspector] [ReadOnly]
        private string _lastError;

        // Runtime 快取
        [NonSerialized] private object _cachedValue;

        [NonSerialized] private float _lastCacheTime;

        [NonSerialized] private Type _cachedValueType;

        [NonSerialized] private bool _hasValidatedChain = false;

        /// <summary>
        /// 存取鏈是否有效
        /// </summary>
        public bool IsChainValid => _isChainValid;

        /// <summary>
        /// 最後的錯誤訊息
        /// </summary>
        public string LastError => _lastError;

        /// <summary>
        /// IValueProvider.ValueType 實作
        /// </summary>
        public Type ValueType
        {
            get
            {
                if (_cachedValueType != null)
                    return _cachedValueType;

                if (_accessChain != null && _accessChain.IsValid)
                {
                    _cachedValueType = _accessChain.FinalOutputType;
                    return _cachedValueType;
                }

                return typeof(object);
            }
        }

        /// <summary>
        /// IValueProvider.Description 實作
        /// </summary>
        public string Description
        {
            get
            {
                if (_accessChain != null) return $"ChainedValue: {_accessChain.GetAccessPath()}";
                return "未設定存取鏈";
            }
        }

        /// <summary>
        /// IVariableProvider.VarRaw 實作
        /// </summary>
        public AbstractMonoVariable VarRaw => _variableProvider?.VarRaw;

        /// <summary>
        /// IVariableProvider.GetValueType 實作
        /// </summary>
        public Type GetValueType => ValueType;

        private void Awake()
        {
            ValidateSetup();
        }

        private void Start()
        {
            // 在 Start 時進行最終驗證
            if (!ValidateSetup()) Debug.LogError($"ChainedValueProvider 設定無效: {_lastError}", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 編輯器模式下自動驗證
            ValidateSetup();
        }
#endif

        /// <summary>
        /// 當存取鏈改變時的回調
        /// </summary>
        private void OnAccessChainChanged()
        {
            _hasValidatedChain = false;
            _cachedValue = null;
            _cachedValueType = null;
            _lastCacheTime = 0;
            ValidateSetup();
        }

        /// <summary>
        /// 驗證組件設定
        /// </summary>
        [Button("驗證設定")]
        public bool ValidateSetup()
        {
            _lastError = "";
            _isChainValid = false;

            // 檢查存取鏈
            if (_accessChain == null)
            {
                _lastError = "存取鏈未設定";
                return false;
            }

            // 檢查變數提供者
            if (_variableProvider == null)
            {
                _lastError = "變數提供者未設定";
                return false;
            }

            // 使用型別安全驗證器進行全面驗證
            var chainValidationResult = TypeSafetyValidator.ValidateAccessChain(_accessChain);
            if (!chainValidationResult.IsValid)
            {
                _lastError = $"存取鏈型別驗證失敗: {chainValidationResult.ErrorMessage}";
                return false;
            }

            var providerValidationResult = TypeSafetyValidator.ValidateChainedValueProvider(this);
            if (!providerValidationResult.IsValid)
            {
                _lastError = $"組件驗證失敗: {providerValidationResult.ErrorMessage}";
                return false;
            }

            // 檢查變數標籤與變數提供者的相容性
            if (_accessChain.VariableTag != null && _variableProvider.VarRaw != null)
            {
                var expectedType = _accessChain.VariableTag.ValueType;
                var actualType = _variableProvider.ValueType;

                if (expectedType != null && actualType != null &&
                    !TypeCompatibilityChecker.AreCompatible(actualType, expectedType))
                {
                    var suggestions = TypeCompatibilityChecker.GetConversionSuggestions(actualType, expectedType);
                    _lastError = $"變數類型不相容。期望: {expectedType.Name}, 實際: {actualType.Name}。" +
                                 $"建議: {string.Join("; ", suggestions)}";
                    return false;
                }
            }

            _isChainValid = true;
            _hasValidatedChain = true;
            return true;
        }

        /// <summary>
        /// 取得原始值（不使用快取）
        /// </summary>
        private object GetRawValue()
        {
            // 確保設定有效
            if (!_hasValidatedChain && !ValidateSetup()) return null;

            if (!_isChainValid) return null;

            try
            {
                // 從變數提供者取得變數值
                var variableValue = _variableProvider.VarRaw?.GetValue<object>();
                if (variableValue == null)
                {
                    Debug.LogWarning("變數值為 null", this);
                    return null;
                }

                // 執行存取鏈
                var result = _accessChain.ExecuteChain(variableValue);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"執行存取鏈時發生錯誤: {ex.Message}", this);
                return null;
            }
        }

        /// <summary>
        /// 取得值（可能使用快取）
        /// </summary>
        private object GetValueInternal()
        {
            // 檢查是否需要使用快取
            if (!_enableValueCaching || _cacheValidDuration <= 0) return GetRawValue();

            // 檢查快取是否仍然有效
            var currentTime = Time.time;
            if (_cachedValue != null && currentTime - _lastCacheTime < _cacheValidDuration) return _cachedValue;

            // 更新快取
            _cachedValue = GetRawValue();
            _lastCacheTime = currentTime;
            return _cachedValue;
        }

        /// <summary>
        /// IValueProvider.Get<T>() 實作
        /// </summary>
        public T Get<T>()
        {
            var value = GetValueInternal();

            if (value is T result)
                return result;

            if (value == null)
                return default;

            // 嘗試型別轉換
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogError($"無法將 {value.GetType().Name} 轉換為 {typeof(T).Name}: {ex.Message}", this);
                return default;
            }
        }

        /// <summary>
        /// IVariableProvider.GetVar<TVariable>() 實作
        /// </summary>
        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _variableProvider?.GetVar<TVariable>();
        }

        /// <summary>
        /// 強制重新整理快取
        /// </summary>
        [Button("重新整理快取")]
        public void RefreshCache()
        {
            _cachedValue = null;
            _lastCacheTime = 0;
            _cachedValueType = null;

            // 重新驗證並取得值
            ValidateSetup();
            if (_isChainValid)
            {
                var newValue = GetValueInternal();
                Debug.Log($"快取已重新整理，新值: {newValue}", this);
            }
        }

        /// <summary>
        /// 取得除錯資訊
        /// </summary>
        [Button("顯示除錯資訊")]
        public void ShowDebugInfo()
        {
            Debug.Log($"=== ChainedValueProvider 除錯資訊 ===", this);
            Debug.Log($"存取鏈: {(_accessChain != null ? _accessChain.name : "未設定")}", this);
            Debug.Log($"變數提供者: {(_variableProvider != null ? _variableProvider.GetType().Name : "未設定")}", this);
            Debug.Log($"鏈式有效: {_isChainValid}", this);
            Debug.Log($"最後錯誤: {_lastError}", this);
            Debug.Log($"值類型: {ValueType?.Name ?? "Unknown"}", this);
            Debug.Log($"當前值: {GetValueInternal()}", this);

            if (_accessChain != null) Debug.Log($"存取路徑: {_accessChain.GetAccessPath()}", this);
        }

        /// <summary>
        /// 產生型別安全性報告
        /// </summary>
        [Button("產生型別安全性報告")]
        public void GenerateTypeSafetyReport()
        {
            if (_accessChain == null)
            {
                Debug.Log("無存取鏈，無法產生報告", this);
                return;
            }

            var report = TypeSafetyValidator.GenerateTypeSafetyReport(_accessChain);
            Debug.Log($"=== ChainedValueProvider 型別安全性報告 ===\n{report}", this);
        }

        /// <summary>
        /// 檢查型別相容性
        /// </summary>
        [Button("檢查型別相容性")]
        public void CheckTypeCompatibility()
        {
            if (!_isChainValid)
            {
                Debug.LogWarning("存取鏈無效，無法檢查型別相容性", this);
                return;
            }

            var currentValue = GetValueInternal();
            var actualType = currentValue?.GetType();
            var expectedType = ValueType;

            Debug.Log($"=== 型別相容性檢查 ===", this);
            Debug.Log($"期望型別: {expectedType?.Name ?? "Unknown"}", this);
            Debug.Log($"實際型別: {actualType?.Name ?? "null"}", this);

            if (actualType != null && expectedType != null)
            {
                var isCompatible = TypeCompatibilityChecker.AreCompatible(actualType, expectedType);
                var compatibilityScore = TypeCompatibilityChecker.GetCompatibilityScore(actualType, expectedType);

                Debug.Log($"相容性: {(isCompatible ? "✓ 相容" : "✗ 不相容")}", this);
                Debug.Log($"相容性分數: {compatibilityScore}/100", this);

                if (!isCompatible)
                {
                    var suggestions = TypeCompatibilityChecker.GetConversionSuggestions(actualType, expectedType);
                    Debug.Log($"轉換建議: {string.Join("; ", suggestions)}", this);
                }
            }
        }

        /// <summary>
        /// 同步存取鏈中的所有欄位名稱
        /// </summary>
        [Button("同步存取鏈欄位名稱")]
        public void SyncAccessChainFieldNames()
        {
            if (_accessChain == null)
            {
                Debug.LogWarning("存取鏈未設定，無法同步", this);
                return;
            }

            Debug.Log("=== 同步 ChainedValueProvider 的存取鏈欄位名稱 ===", this);
            _accessChain.SyncAllFieldNames();
            
            // 重新驗證設定
            ValidateSetup();
            
            Debug.Log("存取鏈欄位名稱同步完成", this);
        }

        /// <summary>
        /// 檢查存取鏈的欄位名稱同步狀態
        /// </summary>
        [Button("檢查存取鏈同步狀態")]
        public void CheckAccessChainSync()
        {
            if (_accessChain == null)
            {
                Debug.LogWarning("存取鏈未設定，無法檢查", this);
                return;
            }

            Debug.Log("=== 檢查 ChainedValueProvider 的存取鏈同步狀態 ===", this);
            _accessChain.CheckAllFieldNamesSync();
        }

        /// <summary>
        /// 設定存取鏈（程式碼使用）
        /// </summary>
        public void SetAccessChain(ValueAccessChain accessChain)
        {
            _accessChain = accessChain;
            OnAccessChainChanged();
        }

        /// <summary>
        /// 設定變數提供者（程式碼使用）
        /// </summary>
        public void SetVariableProvider(AbstractVariableProviderRef variableProvider)
        {
            _variableProvider = variableProvider;
            _hasValidatedChain = false;
            ValidateSetup();
        }

        /// <summary>
        /// 檢查是否可以轉換為指定類型
        /// </summary>
        public bool CanConvertTo<T>()
        {
            try
            {
                var value = GetValueInternal();
                if (value is T)
                    return true;

                if (value != null)
                {
                    Convert.ChangeType(value, typeof(T));
                    return true;
                }

                return typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null;
            }
            catch
            {
                return false;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 編輯器預覽資訊
        /// </summary>
        [ShowInInspector]
        [PreviewInInspector]
        [PropertyOrder(1000)]
        [MultiLineProperty(5)]
        [ReadOnly]
        private string EditorPreview
        {
            get
            {
                if (!Application.isPlaying) return "請在執行時檢視即時數值";

                var preview = $"狀態: {(_isChainValid ? "✓ 有效" : "✗ 無效")}\n";

                if (!string.IsNullOrEmpty(_lastError)) preview += $"錯誤: {_lastError}\n";

                if (_isChainValid)
                {
                    var currentValue = GetValueInternal();
                    preview += $"當前值: {currentValue ?? "null"}\n";
                    preview += $"值類型: {currentValue?.GetType().Name ?? "null"}\n";

                    if (_enableValueCaching)
                    {
                        var timeSinceCache = Time.time - _lastCacheTime;
                        preview += $"快取狀態: {(timeSinceCache < _cacheValidDuration ? "有效" : "過期")}";
                    }
                }

                return preview;
            }
        }
#endif
    }
}