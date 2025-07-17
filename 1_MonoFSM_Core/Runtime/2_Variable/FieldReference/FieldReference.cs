using System;
using System.Reflection;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.TypeTag;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// ScriptableObject 基於的欄位引用，使用 MetadataToken 提供 refactor-safe 的欄位存取
    /// </summary>
    [CreateAssetMenu(menuName = "RCG/Field Reference", fileName = "New Field Reference")]
    public class FieldReference : ScriptableObject, IStringKey
    {
        [Header("來源類型")] [SerializeField] [OnValueChanged(nameof(OnSourceTypeChanged))]
        private MySerializedType<MonoBehaviour> _sourceType = new();

        [SerializeField] [OnValueChanged(nameof(OnSourceTypeChanged))]
        private AbstractTypeTag _sourceTypeTag;

        [Header("欄位選擇")]
        [SerializeField]
        [ValueDropdown(nameof(GetAvailableFields))]
        [OnValueChanged(nameof(OnFieldChanged))]
        private string _fieldName;

        [Header("Refactor-Safe 資料")] [SerializeField] [PreviewInInspector] [ReadOnly]
        private string _assemblyQualifiedTypeName;

        // [SerializeField] [PreviewInInspector] [ReadOnly]
        // private int _metadataToken;

        [SerializeField] [PreviewInInspector] [ReadOnly]
        private bool _isProperty;

        [Header("欄位資訊")] [SerializeField] [PreviewInInspector] [ReadOnly]
        private MySerializedType<object> _fieldType = new();

        [SerializeField] [PreviewInInspector] [ReadOnly]
        private bool _isArray;

        [SerializeField] [PreviewInInspector] [ReadOnly]
        private bool _canRead = true;

        [SerializeField] [PreviewInInspector] [ReadOnly]
        private bool _canWrite;

        [Header("說明")] [TextArea] public string Note;

        // Runtime 快取
        [NonSerialized] private MemberInfo _cachedMemberInfo;

        [NonSerialized] private Func<object, object> _cachedGetter;

        [NonSerialized] private string _cachedStringKey;

        /// <summary>
        /// 取得來源類型
        /// </summary>
        public Type SourceType => _sourceType.RestrictType;

        /// <summary>
        /// 取得欄位名稱
        /// </summary>
        public string FieldName => _fieldName;

        /// <summary>
        /// 取得欄位類型
        /// </summary>
        public Type FieldType => _fieldType.RestrictType;

        /// <summary>
        /// 是否為陣列
        /// </summary>
        public bool IsArray => _isArray;

        /// <summary>
        /// 是否可讀取
        /// </summary>
        public bool CanRead => _canRead;

        /// <summary>
        /// 是否可寫入
        /// </summary>
        public bool CanWrite => _canWrite;

        /// <summary>
        /// 是否為屬性（否則為欄位）
        /// </summary>
        public bool IsProperty => _isProperty;

        public string GetStringKey
        {
            get
            {
                if (_cachedStringKey == null)
                    _cachedStringKey = $"{name}_{_sourceType.RestrictType?.Name}_{_fieldName}";
                return _cachedStringKey;
            }
        }

        /// <summary>
        /// 取得可用欄位列表（用於下拉選單）
        /// </summary>
        private ValueDropdownList<string> GetAvailableFields()
        {
            var dropdown = new ValueDropdownList<string>();

            if (SourceType == null)
            {
                dropdown.Add("請先選擇來源類型", "");
                return dropdown;
            }

            // 取得所有可讀取的屬性
            var properties = SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .OrderBy(p => p.Name);

            foreach (var prop in properties)
            {
                var displayName = $"{prop.Name} : {GetFriendlyTypeName(prop.PropertyType)}";
                if (prop.PropertyType.IsArray)
                    displayName += " (Array)";
                dropdown.Add(displayName, prop.Name);
            }

            // 取得所有公開欄位
            var fields = SourceType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(f => f.Name);

            foreach (var field in fields)
            {
                var displayName = $"{field.Name} : {GetFriendlyTypeName(field.FieldType)}";
                if (field.FieldType.IsArray)
                    displayName += " (Array)";
                dropdown.Add(displayName, field.Name);
            }

            if (dropdown.Count == 0) dropdown.Add("無可用欄位", "");

            return dropdown;
        }

        /// <summary>
        /// 取得友善的類型名稱顯示
        /// </summary>
        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(float)) return "float";
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type.IsArray) return GetFriendlyTypeName(type.GetElementType()) + "[]";
            return type.Name;
        }

        /// <summary>
        /// 當來源類型改變時的回調
        /// </summary>
        private void OnSourceTypeChanged()
        {
            _fieldName = "";
            _cachedMemberInfo = null;
            _cachedGetter = null;
            _cachedStringKey = null;
            ClearFieldInfo();
        }

        /// <summary>
        /// 當欄位改變時的回調
        /// </summary>
        private void OnFieldChanged()
        {
            _cachedMemberInfo = null;
            _cachedGetter = null;
            _cachedStringKey = null;
            UpdateFieldInfo();
        }

        /// <summary>
        /// 更新欄位資訊
        /// </summary>
        private void UpdateFieldInfo()
        {
            if (SourceType == null || string.IsNullOrEmpty(_fieldName))
            {
                ClearFieldInfo();
                return;
            }

            // 嘗試取得屬性
            var property = SourceType.GetProperty(_fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                _isProperty = true;
                // _metadataToken = property.MetadataToken;
                _assemblyQualifiedTypeName = SourceType.AssemblyQualifiedName;
                _fieldType.SetType(property.PropertyType);
                _isArray = property.PropertyType.IsArray;
                _canRead = property.CanRead;
                _canWrite = property.CanWrite;
                return;
            }

            // 嘗試取得欄位
            var field = SourceType.GetField(_fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                _isProperty = false;
                // _metadataToken = field.MetadataToken;
                _assemblyQualifiedTypeName = SourceType.AssemblyQualifiedName;
                _fieldType.SetType(field.FieldType);
                _isArray = field.FieldType.IsArray;
                _canRead = true;
                _canWrite = !field.IsInitOnly;
                return;
            }

            // 找不到欄位
            ClearFieldInfo();
            Debug.LogWarning($"在類型 {SourceType.Name} 中找不到欄位或屬性 '{_fieldName}'", this);
        }

        /// <summary>
        /// 清除欄位資訊
        /// </summary>
        private void ClearFieldInfo()
        {
            _assemblyQualifiedTypeName = "";
            // _metadataToken = 0;
            _isProperty = false;
            _fieldType.SetType(null);
            _isArray = false;
            _canRead = false;
            _canWrite = false;
        }

        /// <summary>
        /// 取得 MemberInfo（使用 RefactorSafeNameResolver 進行 attribute-based refactor-safe 查找）
        /// </summary>
        public MemberInfo GetMemberInfo()
        {
            if (_cachedMemberInfo != null)
            {
                Debug.Log($"使用快取的 MemberInfo: {_cachedMemberInfo.Name}", this);
                return _cachedMemberInfo;
            }

            if (SourceType == null || string.IsNullOrEmpty(_fieldName))
            {
                Debug.LogWarning("SourceType 或 _fieldName 為空，無法查找成員", this);
                return null;
            }

            // 🆕 優先使用 RefactorSafeNameResolver 進行 attribute-based 查找
            Debug.Log($"使用 RefactorSafeNameResolver 查找成員: {SourceType.Name}.{_fieldName}", this);
            _cachedMemberInfo = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(SourceType, _fieldName);

            if (_cachedMemberInfo != null)
            {
                // 🆕 自動同步欄位名稱：如果通過 attribute-based 查找找到的成員名稱與儲存的不同，自動更新
                if (_cachedMemberInfo.Name != _fieldName)
                {
                    Debug.Log($"檢測到欄位重構：'{_fieldName}' -> '{_cachedMemberInfo.Name}'，自動更新欄位名稱", this);
                    _fieldName = _cachedMemberInfo.Name;
                    _cachedStringKey = null; // 清除字串快取

#if UNITY_EDITOR
                    // 在編輯器中標記為 dirty，確保變更被保存
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }

                // 同步 MetadataToken 資訊（保留向後相容性）
                // _metadataToken = _cachedMemberInfo.MetadataToken;
                _assemblyQualifiedTypeName = SourceType.AssemblyQualifiedName;

                return _cachedMemberInfo;
            }

            // // 回退方案：如果有 MetadataToken，嘗試使用
            // if (!string.IsNullOrEmpty(_assemblyQualifiedTypeName))
            //     try
            //     {
            //         Debug.Log($"RefactorSafeNameResolver 查找失敗，回退到 MetadataToken {_metadataToken}", this);
            //         var type = Type.GetType(_assemblyQualifiedTypeName);
            //         if (type != null)
            //         {
            //             var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            //             _cachedMemberInfo = members.FirstOrDefault(m => m.MetadataToken == _metadataToken);
            //             if (_cachedMemberInfo != null) return _cachedMemberInfo;
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         Debug.LogError($"MetadataToken 回退查找失敗: {ex.Message}", this);
            //     }

            // 最終回退：標準名稱查找
            // Debug.LogWarning($"RefactorSafeNameResolver 和 MetadataToken 都失敗，回退到標準名稱查找", this);
            return GetMemberInfoByName();
        }

        /// <summary>
        /// 用名稱查找 MemberInfo（回退方案）
        /// </summary>
        private MemberInfo GetMemberInfoByName()
        {
            if (SourceType == null || string.IsNullOrEmpty(_fieldName))
                return null;

            // 嘗試屬性
            var property = SourceType.GetProperty(_fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                _cachedMemberInfo = property;
                return _cachedMemberInfo;
            }

            // 嘗試欄位
            var field = SourceType.GetField(_fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                _cachedMemberInfo = field;
                return _cachedMemberInfo;
            }

            return null;
        }

        /// <summary>
        /// 取得值
        /// </summary>
        public object GetValue(object source)
        {
            if (source == null)
                return null;

            var memberInfo = GetMemberInfo();
            if (memberInfo == null)
                return null;

            try
            {
                if (memberInfo is PropertyInfo property)
                {
                    if (!property.CanRead)
                    {
                        Debug.LogError($"屬性 {property.Name} 不可讀取", this);
                        return null;
                    }

                    return property.GetValue(source);
                }

                if (memberInfo is FieldInfo field) return field.GetValue(source);

                Debug.LogError($"不支援的成員類型: {memberInfo.GetType()}", this);
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"取得值失敗: {ex.Message}", this);
                return null;
            }
        }

        /// <summary>
        /// 取得泛型值
        /// </summary>
        public T GetValue<T>(object source)
        {
            var value = GetValue(source);
            if (value is T result)
                return result;

            if (value == null)
                return default;

            Debug.LogError($"無法將 {value.GetType()} 轉換為 {typeof(T)}", this);
            return default;
        }

        /// <summary>
        /// 驗證此 FieldReference 是否有效，並自動同步欄位名稱
        /// </summary>
        [Button("驗證欄位引用")]
        public bool ValidateReference()
        {
            if (SourceType == null)
            {
                Debug.LogError("來源類型未設定", this);
                return false;
            }

            if (string.IsNullOrEmpty(_fieldName))
            {
                Debug.LogError("欄位名稱未設定", this);
                return false;
            }

            // 清除快取，強制重新檢查
            _cachedMemberInfo = null;

            var memberInfo = GetMemberInfo();
            if (memberInfo == null)
            {
                Debug.LogError($"在類型 {SourceType.Name} 中找不到欄位 '{_fieldName}'", this);
                return false;
            }

            Debug.Log($"欄位引用有效: {SourceType.Name}.{_fieldName}", this);
            return true;
        }

        /// <summary>
        /// 重新整理 MetadataToken 並同步欄位名稱（用於修復 refactor 後的問題）
        /// </summary>
        [Button("重新整理 MetadataToken 和欄位名稱")]
        public void RefreshMetadataToken()
        {
            var originalFieldName = _fieldName;

            _cachedMemberInfo = null;
            _cachedGetter = null;
            _cachedStringKey = null;

            // 重新驗證並可能自動同步欄位名稱
            var isValid = ValidateReference();

            if (isValid)
            {
                if (originalFieldName != _fieldName)
                    Debug.Log($"欄位名稱已自動同步：'{originalFieldName}' -> '{_fieldName}'", this);
                // else
                //     Debug.Log($"欄位引用有效，MetadataToken: {_metadataToken}", this);
            }
            else
            {
                Debug.LogWarning("欄位引用驗證失敗，請檢查設定", this);
            }
        }

        /// <summary>
        /// 檢查欄位名稱同步狀態（使用 attribute-based 和 MetadataToken）
        /// </summary>
        [Button("檢查欄位名稱同步")]
        public void CheckFieldNameSync()
        {
            if (SourceType == null || string.IsNullOrEmpty(_fieldName))
            {
                Debug.LogWarning("SourceType 或 _fieldName 為空，無法檢查同步", this);
                return;
            }

            Debug.Log("=== 檢查欄位名稱同步狀態 ===", this);

            // 1. 檢查 attribute-based 查找
            var memberByAttribute = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(SourceType, _fieldName);
            if (memberByAttribute != null)
            {
                if (memberByAttribute.Name != _fieldName)
                {
                    Debug.Log($"[Attribute-based] 檢測到欄位重構：儲存='{_fieldName}', 實際='{memberByAttribute.Name}'", this);

                    // 顯示重構歷史
                    var trackingInfo = RefactorSafeNameResolver.GetMemberTrackingInfo(memberByAttribute);
                    if (trackingInfo.HasFormerNames)
                        Debug.Log(
                            $"欄位 {memberByAttribute.Name} 有重構歷史：{string.Join(", ", trackingInfo.FormerNames.Select(f => f.Name))}",
                            this);
                }
                else
                {
                    Debug.Log($"[Attribute-based] 欄位名稱已同步：'{_fieldName}'", this);
                }
            }
            else
            {
                Debug.Log($"[Attribute-based] 找不到欄位 '{_fieldName}'", this);
            }

            // 2. 檢查 MetadataToken（如果有的話）
            // if (_metadataToken != 0 && !string.IsNullOrEmpty(_assemblyQualifiedTypeName))
            // {
            //     try
            //     {
            //         var type = Type.GetType(_assemblyQualifiedTypeName);
            //         if (type != null)
            //         {
            //             var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            //             var actualMember = members.FirstOrDefault(m => m.MetadataToken == _metadataToken);
            //
            //             if (actualMember != null)
            //             {
            //                 if (actualMember.Name != _fieldName)
            //                 {
            //                     Debug.Log($"[MetadataToken] 檢測到欄位名稱不同步：儲存='{_fieldName}', 實際='{actualMember.Name}'", this);
            //                 }
            //                 else
            //                 {
            //                     Debug.Log($"[MetadataToken] 欄位名稱已同步：'{_fieldName}'", this);
            //                 }
            //             }
            //             else
            //             {
            //                 Debug.LogWarning($"[MetadataToken] 找不到 MetadataToken {_metadataToken} 對應的成員", this);
            //             }
            //         }
            //         else
            //         {
            //             Debug.LogError($"[MetadataToken] 找不到類型: {_assemblyQualifiedTypeName}", this);
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         Debug.LogError($"[MetadataToken] 檢查同步時發生錯誤: {ex.Message}", this);
            //     }
            // }
            // else
            // {
            //     Debug.Log("[MetadataToken] 沒有有效的 MetadataToken", this);
            // }

            // Debug.Log("建議使用 '重新整理 MetadataToken 和欄位名稱' 按鈕進行同步", this);
        }

        /// <summary>
        /// 🆕 檢查此欄位的 attribute-based 追踪資訊
        /// </summary>
        [Button("檢查 Attribute 追踪資訊")]
        public void CheckAttributeTrackingInfo()
        {
            if (SourceType == null || string.IsNullOrEmpty(_fieldName))
            {
                Debug.LogWarning("SourceType 或 _fieldName 為空，無法檢查追踪資訊", this);
                return;
            }

            Debug.Log("=== Attribute-based 追踪資訊 ===", this);

            // 檢查來源類型的追踪資訊
            var typeTrackingInfo = RefactorSafeNameResolver.GetTypeTrackingInfo(SourceType);
            Debug.Log($"來源類型: {SourceType.FullName}", this);
            if (typeTrackingInfo.HasFormerNames)
                Debug.Log($"類型重構歷史: {string.Join(", ", typeTrackingInfo.FormerNames.Select(f => f.Name))}", this);
            else
                Debug.Log("Class沒有重構歷史", this);

            // 檢查欄位的追踪資訊
            var member = RefactorSafeNameResolver.FindMemberByCurrentOrFormerName(SourceType, _fieldName);
            if (member != null)
            {
                var memberTrackingInfo = RefactorSafeNameResolver.GetMemberTrackingInfo(member);
                Debug.Log($"欄位: {member.Name}", this);
                if (memberTrackingInfo.HasFormerNames)
                    foreach (var formerName in memberTrackingInfo.FormerNames)
                    {
                        var info = $"前名稱: {formerName.Name}";
                        if (!string.IsNullOrEmpty(formerName.Version)) info += $", 版本: {formerName.Version}";
                        if (!string.IsNullOrEmpty(formerName.Reason)) info += $", 原因: {formerName.Reason}";
                        Debug.Log(info, this);
                    }
                else
                    Debug.Log("欄位沒有重構歷史", this);
            }
            else
            {
                Debug.LogWarning($"找不到欄位 '{_fieldName}'", this);
            }
        }
    }
}