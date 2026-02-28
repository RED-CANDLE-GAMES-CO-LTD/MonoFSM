using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    [Serializable]
    public class MySerializedType : MySerializedType<object> { }

    //EditorOnly
    //T 表示這個type可以
    //兩個Type, 一個filter用，一個實際使用的
    [Serializable]
    public class MySerializedType<T> : ISerializationCallbackReceiver
    {
        [HideInInspector]
        [NonSerialized]
        public Object _bindObject; //debug用

        //override baseType
        [FormerlySerializedAs("_baseVarTypeName")]
        [FormerlySerializedAs("_varTypeName")]
        [SerializeField]
        [PreviewInDebugMode]
        private string _baseFilterTypeName;

        private Type _baseFilterType; //default 用 T?

        // 🆕 Refactor-Safe 支援：MetadataToken 機制
        // [Header("Refactor-Safe 資料")] [SerializeField] [PreviewInInspector] [ReadOnly]
        // private int _typeMetadataToken;

        [ShowInDebugMode]
        [SerializeField]
        [ReadOnly]
        private string _typeFullName; // 用於顯示和驗證 //兩種都有？搞屁？

        [ShowInDebugMode]
        [SerializeField]
        [ReadOnly]
        private string _assemblyName;

        public void SetBaseType(Type type)
        {
            if (type == null)
                return;
            _baseFilterType = type;
            _baseFilterTypeName = type.AssemblyQualifiedName;
        }

        [PreviewInDebugMode]
        public Type BaseFilterType
        {
            get
            {
                if (_baseFilterType == null && !string.IsNullOrEmpty(_baseFilterTypeName))
                    _baseFilterType = Type.GetType(_baseFilterTypeName);
                if (_baseFilterType != null)
                    return _baseFilterType;
                else
                    return typeof(T); //如果沒有設定，回傳T
            }
            set
            {
                _baseFilterType = value;
                _baseFilterTypeName = value?.AssemblyQualifiedName;
            }
        }

        // [Button]
        // void GetTypeFromString()
        // {
        //     if (typeName.IsNullOrWhitespace())
        //         return;
        //     _type = Type.GetType(typeName);
        // }
        public bool IsTypeSet => _type != null;
        private Type _type; //cached

        private bool FilterTypes(Type type)
        {
            if (BaseFilterType == null)
                return true;
            return BaseFilterType.IsAssignableFrom(type);
        }

        public void SetType(Type type)
        {
            _type = type;
            typeName = _type?.AssemblyQualifiedName ?? typeName;

            // 🆕 同步 MetadataToken 資訊
            if (_type != null)
            {
                // _typeMetadataToken = _type.MetadataToken;
                _typeFullName = _type.FullName;
                _assemblyName = _type.Assembly.GetName().Name;
            }
            else
            {
                // _typeMetadataToken = 0;
                _typeFullName = "";
                _assemblyName = "";
            }

            // Debug.Log($"SetType: {_type}");
        }

        // [Header("宣告型別：")]

        [ShowInDebugMode]
        // [OnValueChanged(nameof(TypeToString))]
        [TypeSelectorSettings(FilterTypesFunction = nameof(FilterTypes))]
        public Type RestrictType
        {
            get
            {
                if (_type == null)
                {
                    // var resolvedType = GetTypeByMetadataTokenOrName();
                    // if (resolvedType != null)
                    // {
                    //     _type = resolvedType;
                    //     // 檢查並同步型別名稱
                    //     SyncTypeNameIfNeeded();
                    // }
                    return BaseFilterType; //default用程式碼的
                }
                return _type;
            }
            set
            {
                _type = value;
                typeName = _type?.AssemblyQualifiedName ?? typeName;

                // 🆕 同步 MetadataToken 資訊
                if (_type != null)
                {
                    // _typeMetadataToken = _type.MetadataToken;
                    _typeFullName = _type.FullName;
                    _assemblyName = _type.Assembly.GetName().Name;
                }
                else
                {
                    // _typeMetadataToken = 0;
                    _typeFullName = "";
                    _assemblyName = "";
                }
                // TypeToString();
            }
        }

        //
        // void TypeToString()
        // {
        //     if (_type == null)
        //         return;
        //     typeName = _type.ToString();
        // }

        bool IsTypeMissing => _type == null && typeName.IsNullOrWhitespace() == false;

        [InfoBox("type is not exist, reselect", InfoMessageType.Error, nameof(IsTypeMissing))]
        [Required]
        [ShowInDebugMode]
        [SerializeField]
        private string typeName; //這個是full，太難了？

        [ShowInInspector]
        [HideLabel]
        [DisplayAsString]
        public string TypeName
        {
            get => _type?.Name;
            private set => throw new NotImplementedException();
        }

        public void OnBeforeSerialize()
        {
            typeName = _type?.AssemblyQualifiedName ?? typeName;
            _baseFilterTypeName = _baseFilterType?.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize() //FIXME: 這個會讓reload domain變慢？資料變多就會跑愈多？
        {
            if (typeName.IsNullOrWhitespace())
            {
                _type = null;
            }
            else
            {
                // 🆕 優先使用 MetadataToken 進行解析
                _type = GetTypeByMetadataTokenOrName();
                if (_type == null)
#if UNITY_EDITOR
                    Debug.LogError(
                        $"Type '{typeName}' could not be found. Please check the type name.",
                        _bindObject
                    ); //沒辦法拿到data holder...煩
#else
                    Debug.LogError(
                        $"Type '{typeName}' could not be found. Please check the type name."
                    );
#endif
            }

            _baseFilterType = string.IsNullOrEmpty(_baseFilterTypeName)
                ? null
                : Type.GetType(_baseFilterTypeName);
        }

        /// <summary>
        /// 🆕 使用 RefactorSafeNameResolver 或名稱查找型別（替代 MetadataToken 機制）
        /// </summary>
        private Type GetTypeByMetadataTokenOrName()
        {
            // 優先使用 RefactorSafeNameResolver 進行 attribute-based 查找
            if (!string.IsNullOrEmpty(typeName))
            {
#if UNITY_EDITOR
                var type = RefactorSafeNameResolver.FindTypeByCurrentOrFormerName(
                    typeName,
                    _assemblyName,
                    _bindObject
                );
#else
                var type = RefactorSafeNameResolver.FindTypeByCurrentOrFormerName(
                    typeName,
                    _assemblyName,
                    null
                );
#endif
                // Debug.Log($"RefactorSafeNameResolver 查找型別 '{typeName}' 結果：{type?.FullName ?? "未找到"}", _bindObject);
                if (type != null)
                {
                    // 同步 MetadataToken 資訊（保留向後相容性）
                    // _typeMetadataToken = type.MetadataToken;
                    _assemblyName = type.Assembly.GetName().Name;
                    return type;
                }
            }

            // 最終回退：直接用名稱查找
#if UNITY_EDITOR
            Debug.LogError($"RefactorSafeNameResolver 無法找到型別 '{typeName}'", _bindObject);
#else
            Debug.LogError($"RefactorSafeNameResolver 無法找到型別 '{typeName}'");
#endif
            // Debug.LogWarning($"使用 RefactorSafeNameResolver 和 MetadataToken 都失敗，回退到標準名稱查找: {typeName}");
            return null;
        }

        /// <summary>
        /// 🆕 檢查並同步型別名稱（增強 attribute-based 支援）
        /// </summary>
        private void SyncTypeNameIfNeeded()
        {
            if (_type == null)
                return;

            var currentFullName = _type.FullName;
            var currentAssemblyQualifiedName = _type.AssemblyQualifiedName;

            // 檢查 FullName 是否有變化
            if (_typeFullName != currentFullName)
            {
                Debug.Log(
                    $"檢測到型別重構：'{_typeFullName}' -> '{currentFullName}'，自動更新型別名稱"
                );

                // 檢查是否有 FormerlyNamedAs 或 FormerlyFullName 屬性
                var trackingInfo = RefactorSafeNameResolver.GetTypeTrackingInfo(_type);
                if (trackingInfo.HasFormerNames)
                    Debug.Log($"型別 {currentFullName} 有重構歷史，attribute-based 追踪可用");

                // 更新所有相關資訊
                _typeFullName = currentFullName;
                typeName = currentAssemblyQualifiedName;
                _assemblyName = _type.Assembly.GetName().Name;

#if UNITY_EDITOR
                // 在編輯器中標記為 dirty（需要有 UnityEngine.Object 的 context）
                // 注意：MySerializedType 是序列化類別，不是 UnityEngine.Object，所以無法直接標記 dirty
                // 但字串更新會在序列化時自動保存
#endif
            }
        }

        /// <summary>
        /// 🆕 驗證型別引用並同步名稱
        /// </summary>
        // [Button("驗證型別引用")]
        public bool ValidateTypeReference()
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.LogWarning("型別名稱未設定");
                return false;
            }

            // 清除快取，強制重新解析
            _type = null;

            var resolvedType = GetTypeByMetadataTokenOrName();

            if (resolvedType == null)
            {
                Debug.LogError($"無法解析型別: {typeName}");
                return false;
            }

            _type = resolvedType;
            Debug.Log($"型別引用有效: {resolvedType.FullName}");
            return true;
        }

        /// <summary>
        /// 🆕 檢查型別名稱同步狀態
        /// </summary>
        // [Button("檢查型別名稱同步")]
        public void CheckTypeNameSync()
        {
            try
            {
                var actualType = GetTypeByMetadataTokenOrName();
                if (actualType == null)
                {
                    Debug.LogWarning($"找不到 {_typeFullName}  對應的型別");
                    return;
                }

                if (actualType.FullName != _typeFullName)
                {
                    Debug.Log(
                        $"檢測到型別名稱不同步：儲存='{_typeFullName}', 實際='{actualType.FullName}'"
                    );
                    Debug.Log("請使用 '重新整理型別 MetadataToken' 按鈕進行同步");
                }
                else
                {
                    Debug.Log($"型別名稱已同步：'{_typeFullName}'");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"檢查同步時發生錯誤: {ex.Message}");
            }
        }
    }
}
