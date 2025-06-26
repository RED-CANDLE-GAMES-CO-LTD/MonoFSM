using System;
using System.Linq;
using System.Text.RegularExpressions;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.TypeTag;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

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
        //override baseType
        [FormerlySerializedAs("_baseVarTypeName")]
        [PreviewInInspector]
        [FormerlySerializedAs("_varTypeName")]
        [SerializeField]
        private string _baseFilterTypeName;


        private Type _baseFilterType; //default 用 T?

        public void SetBaseType(Type type)
        {
            if (type == null) return;
            _baseFilterType = type;
            _baseFilterTypeName = type.AssemblyQualifiedName;
        }

        [PreviewInInspector]
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
        
        [Button]
        void GetTypeFromString()
        {
            if (typeName.IsNullOrWhitespace())
                return;
            _type = Type.GetType(typeName);
        }

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
            // Debug.Log($"SetType: {_type}");
        }

        [Header("宣告型別：")]
        [ShowInInspector]
        // [OnValueChanged(nameof(TypeToString))]
        [TypeSelectorSettings(FilterTypesFunction = nameof(FilterTypes))]
        public Type RestrictType
        {
            get
            {
                if (_type == null)
                    GetTypeFromString();
                return _type;
            }
            set
            {
                _type = value;
                typeName = _type?.AssemblyQualifiedName ?? typeName;
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
        [PreviewInInspector]
        [SerializeField]
        string typeName;

        public string TypeName => typeName;

        public void OnBeforeSerialize()
        {
            typeName = _type?.AssemblyQualifiedName ?? typeName;
            _baseFilterTypeName = _baseFilterType?.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (typeName.IsNullOrWhitespace())
            {
                _type = null;
            }
            else
            {
                _type = Type.GetType(typeName);
                if (_type == null)
                    Debug.LogError(
                        $"Type '{typeName}' could not be found. Please check the type name."); //沒辦法拿到data holder...煩
            }

            _baseFilterType = string.IsNullOrEmpty(_baseFilterTypeName) ? null : Type.GetType(_baseFilterTypeName);
        }
    }

    public interface IStringKey
    {
        public string GetStringKey { get; }
    }

    [CreateAssetMenu(menuName = "RCG/VariableTag")]
    public class VariableTag : ScriptableObject, IStringKey //, IFloatValue
    {
        [ShowInInspector]
        [DisplayAsString]
        [PropertyOrder(-1)]
        [LabelText("變數綁定型別")]
        public Type VariableMonoType => _variableType.RestrictType;

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
            var variableType = _variableType?.RestrictType;
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
        // private void OnValidate()
        // {
        //     if (StringKey == "")
        //         StringKey = name;
        // }

        // [SerializeField] private string StringKey; //run起來才？cache?
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


        [HideInInlineEditors] [TextArea] public string Note;

        //可以DI標記variable類型，像是血量？要降低對方的血量之類的
        // [InlineProperty]
        [HideInInlineEditors] public MySerializedType<AbstractMonoVariable> _variableType; //我這個variable是什麼型別

        public MySerializedType<object> _valueFilterType;

        // [ShowInInspector]
        // [LabelText("變數數值型別過濾")]
        // [TypeFilter(nameof(GetValueTypeOptions))]
        // [PropertyOrder(170)]
        // public Type ValueTypeSelector
        // {
        //     get => _valueFilterType.RestrictType;
        //     set => _valueFilterType.SetType(value);
        // }

        // public IEnumerable<Type> GetValueTypeOptions()
        // {
        //     if (_variableType?.RestrictType == null) yield break;
        //
        //     var variableMonoType = _variableType.RestrictType;
        //
        //     var currentType = variableMonoType;
        //     while (currentType != null && currentType != typeof(object))
        //     {
        //         if (currentType.IsGenericType)
        //         {
        //             var genericTypeDef = currentType.GetGenericTypeDefinition();
        //             if (genericTypeDef == typeof(GenericMonoVariable<,,>))
        //             {
        //                 yield return currentType.GetGenericArguments()[2];
        //                 yield break;
        //             }
        //
        //             if (genericTypeDef == typeof(GenericUnityObjectVariable<>))
        //             {
        //                 yield return currentType.GetGenericArguments()[0];
        //                 yield break;
        //             }
        //         }
        //
        //         currentType = currentType.BaseType;
        //     }
        // }

//restrict type也放在variable type裡面？
        // public MySerializedType<object> _valueFilterType; //當我是ObjectVariable時，才用的到這個？

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
#endif
    }
}

