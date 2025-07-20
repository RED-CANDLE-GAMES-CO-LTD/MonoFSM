using System;
using System.Linq;
using System.Text.RegularExpressions;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.TypeTag;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    public interface IVariableTagSetter
    {
        VariableTag refVariableTag { get; }
    }

    [Serializable]
    public class MySerializedType : MySerializedType<object>
    {
    }


    //EditorOnly
    //T 表示這個type可以
    //兩個Type, 一個filter用，一個實際使用的
    [Serializable]
    public class MySerializedType<T> : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [HideInInspector] public Object _bindObject; //debug用
#endif
        //override baseType
        [FormerlySerializedAs("_baseVarTypeName")]
        [FormerlySerializedAs("_varTypeName")]
        [SerializeField]
        [ShowInDebugMode]
        private string _baseFilterTypeName;

        private Type _baseFilterType; //default 用 T?

        // 🆕 Refactor-Safe 支援：MetadataToken 機制
        // [Header("Refactor-Safe 資料")] [SerializeField] [PreviewInInspector] [ReadOnly]
        // private int _typeMetadataToken;

        [ShowInDebugMode] [SerializeField] [ReadOnly]
        private string _typeFullName; // 用於顯示和驗證 //兩種都有？搞屁？

        [ShowInDebugMode] [SerializeField] [ReadOnly]
        private string _assemblyName;

        public void SetBaseType(Type type)
        {
            if (type == null) return;
            _baseFilterType = type;
            _baseFilterTypeName = type.AssemblyQualifiedName;
        }

        [ShowInDebugMode]
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
            get => _type.Name;
            private set => throw new NotImplementedException();
        }

        public void OnBeforeSerialize()
        {
            typeName = _type?.AssemblyQualifiedName ?? typeName;
            _baseFilterTypeName = _baseFilterType?.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize() //這個會讓reload domain變慢？資料變多就會跑愈多？
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
                    Debug.LogError(
                        $"Type '{typeName}' could not be found. Please check the type name.",
                        _bindObject); //沒辦法拿到data holder...煩
            }

            _baseFilterType = string.IsNullOrEmpty(_baseFilterTypeName) ? null : Type.GetType(_baseFilterTypeName);
        }

        /// <summary>
        /// 🆕 使用 RefactorSafeNameResolver 或名稱查找型別（替代 MetadataToken 機制）
        /// </summary>
        private Type GetTypeByMetadataTokenOrName()
        {
            // 優先使用 RefactorSafeNameResolver 進行 attribute-based 查找
            if (!string.IsNullOrEmpty(typeName))
            {
                var type = RefactorSafeNameResolver.FindTypeByCurrentOrFormerName(typeName, _assemblyName);
                if (type != null)
                {
                    // 同步 MetadataToken 資訊（保留向後相容性）
                    // _typeMetadataToken = type.MetadataToken;
                    _assemblyName = type.Assembly.GetName().Name;
                    return type;
                }
            }
            
            // 最終回退：直接用名稱查找
            Debug.LogError($"RefactorSafeNameResolver 無法找到型別 '{typeName}'");
            // Debug.LogWarning($"使用 RefactorSafeNameResolver 和 MetadataToken 都失敗，回退到標準名稱查找: {typeName}");
            return null;
        }

        /// <summary>
        /// 🆕 檢查並同步型別名稱（增強 attribute-based 支援）
        /// </summary>
        private void SyncTypeNameIfNeeded()
        {
            if (_type == null) return;

            var currentFullName = _type.FullName;
            var currentAssemblyQualifiedName = _type.AssemblyQualifiedName;

            // 檢查 FullName 是否有變化
            if (_typeFullName != currentFullName)
            {
                Debug.Log($"檢測到型別重構：'{_typeFullName}' -> '{currentFullName}'，自動更新型別名稱");

                // 檢查是否有 FormerlyNamedAs 或 FormerlyFullName 屬性
                var trackingInfo = RefactorSafeNameResolver.GetTypeTrackingInfo(_type);
                if (trackingInfo.HasFormerNames) Debug.Log($"型別 {currentFullName} 有重構歷史，attribute-based 追踪可用");

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
                    Debug.Log($"檢測到型別名稱不同步：儲存='{_typeFullName}', 實際='{actualType.FullName}'");
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

    public interface IStringKey
    {
        public string GetStringKey { get; }
    }

    [CreateAssetMenu(menuName = "RCG/VariableTag")]
    public class VariableTag : ScriptableObject, IStringKey //, IFloatValue , SceneSave?
    {
        private void OnValidate()
        {
            _variableType._bindObject = this;
            _valueFilterType._bindObject = this;
        }

        [ShowInInspector]
        [DisplayAsString]
        [PropertyOrder(-1)]
        [LabelText("變數綁定型別")]
        public Type VariableMonoType => _variableTypeTag?.Type ?? _variableType.RestrictType;

        [FormerlySerializedAs("_variableTypeData")]
        public AbstractTypeTag _variableTypeTag;

        [FormerlySerializedAs("_valueTypeData")]
        public AbstractTypeTag _valueTypeTag;
        //SystemTypeData

        [ShowInInspector]
        [DisplayAsString]
        [PropertyOrder(-1)]
        [LabelText("變數數值型別")]
        public Type ValueType => _valueFilterType.RestrictType;
        //FIXME: 限定型別？
        //FIXME: 下拉式巢狀分類:
        // sampleData? sampleDescriptableTag?
        GameFlagBase SampleData;


        [Button]
        public void SyncValueFilterTypeWithVariableType()
        {
            var variableType = _variableTypeTag?.Type ?? _variableType?.RestrictType;
            if (variableType == null) return;

            Type tValueType = null;
            var currentType = variableType;
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType)
                {
                    var genericTypeDef = currentType.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(GenericMonoVariable<,,>))
                    {
                        tValueType = currentType.GetGenericArguments()[2];
                        break;
                    }

                    if (genericTypeDef == typeof(GenericUnityObjectVariable<>))
                    {
                        tValueType = currentType.GetGenericArguments()[0];
                        break;
                    }
                }

                currentType = currentType.BaseType;
            }

            if (tValueType != null) _valueFilterType.SetBaseType(tValueType);
        }
      
        [Button]
        void RefreshStringKey()
        {
            _cachedStringKey = null;
            var result = GetStringKey;
        }

        //scriptable object會殘留？
        [NonSerialized] string _cachedStringKey;

        [PreviewInInspector]
        public string GetStringKey
        {
            get
            {
                //remove Characters between '[' and ']'

                _cachedStringKey = Regex.Replace(name, @"\[.*?\]", string.Empty);
                _cachedStringKey = Regex.Replace(_cachedStringKey, @"\s+", string.Empty);
                // _cachedStringKey = name.Replace(" ", "");
                return _cachedStringKey;
            }
        }


#if UNITY_EDITOR
        [HideInInlineEditors] [TextArea] public string Note;
#endif

        //可以DI標記variable類型，像是血量？要降低對方的血量之類的
        // [InlineProperty]
        [Obsolete("use _variableTypeTag")]
        [HideInInlineEditors] public MySerializedType<AbstractMonoVariable> _variableType; //我這個variable是什麼型別

        [Obsolete]
        public MySerializedType<object> _valueFilterType;

        public Type ValueFilterType => _valueTypeTag?.Type ?? _valueFilterType.RestrictType;
        
        
        [Button]
        void FetchFilterType()
        {
            //FIXME: 好像拿不到...
        }

        //FIXME: Editor time 把雙向連結撈出來
#if UNITY_EDITOR

        [PreviewInInspector] AbstractMonoVariable[] bindedVariables;

        // [OnInspectorGUI] //會lag?
        [Button]
        void GetBindedVariables()
        {
            bindedVariables = FindObjectsByType<AbstractMonoVariable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(v => v._varTag == this).ToArray();
            bindedVariableSetters = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IVariableTagSetter>()
                .Where(v => v.refVariableTag == this).ToArray();
        }

        [PreviewInInspector] IVariableTagSetter[] bindedVariableSetters;

        /// <summary>
        /// 🆕 同步此 VariableTag 中的所有型別引用
        /// </summary>
        [Button("同步所有型別引用")]
        public void SyncAllTypeReferences()
        {
            RefactorSafeHelper.SyncVariableTagTypes(this);
        }

        /// <summary>
        /// 🆕 檢查此 VariableTag 中的所有型別同步狀態
        /// </summary>
        [Button("檢查型別同步狀態")]
        public void CheckAllTypeReferencesSync()
        {
            RefactorSafeHelper.CheckVariableTagTypesSync(this);
        }
#endif
    }
}

