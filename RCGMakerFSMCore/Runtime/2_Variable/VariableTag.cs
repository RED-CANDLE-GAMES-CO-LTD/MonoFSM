using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public interface IVariableTagSetter
    {
        VariableTag refVariableTag { get; }
    }

    [Serializable]
    public class MySerializedType : MySerializedType<object>
    {
    }


    [Serializable]
    public class MySerializedType<T>
    {
        protected virtual Type varType => typeof(T);

        [Button]
        void GetTypeFromString()
        {
            if (typeName.IsNullOrWhitespace())
                return;
            _type = Type.GetType(typeName);
        }


        // [TypeDrawerSettings(BaseType = typeof(MonoBehaviour))] //FIXME: abstractVariable...每種type要分開寫喔，好煩，attribute的內容要分開，好像也可以啦...
        // [TypeSelectorSettings(FilterTypesFunction = nameof(FilterTypes))]
        // [TypeDrawerSettings]


        private Type _type; //cached

        private bool FilterTypes(Type type)
        {
            return varType.IsAssignableFrom(type);
        }
        // {
        //     var baseType = typeof(T);
        //     return Assembly.GetAssembly(baseType)
        //         .GetTypes()
        //         .Where(t => t.BaseType == baseType && !t.IsAbstract);
        //     // return Assembly.GetAssembly(typeof(Object)).GetTypes().Where(t => varType.IsAssignableFrom(t)).ToArray();
        // }
        // private IEnumerable<Type> FilterTypes()
        // {
        //     var baseType = typeof(T);
        //     return Assembly.GetAssembly(baseType)
        //         .GetTypes()
        //         .Where(t => t.BaseType == baseType && !t.IsAbstract);
        //     // return Assembly.GetAssembly(typeof(Object)).GetTypes().Where(t => varType.IsAssignableFrom(t)).ToArray();
        // }
        //只有type額外定義...

        public void SetType(Type type)
        {
            // Debug.Log("SetType" + type);
            _type = type;
            TypeToString();
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
                TypeToString();
            }
        }

        void TypeToString()
        {
            if (_type == null)
                return;
            typeName = _type.ToString();
        }

        bool IsTypeMissing => _type == null && typeName.IsNullOrWhitespace() == false;

        [InfoBox("type is not exist, reselect", InfoMessageType.Error, nameof(IsTypeMissing))]
        [Required]
        [PreviewInInspector]
        [SerializeField]
        string typeName;

        public string TypeName => typeName;
    }

    public interface IStringKey
    {
        public string GetStringKey { get; }
    }

    [CreateAssetMenu(menuName = "RCG/VariableTag")]
    public class VariableTag : ScriptableObject, IStringKey //, IFloatValue
    {
        //FIXME: 限定型別？
        //FIXME: 下拉式巢狀分類:
        // sampleData? sampleDescriptableTag?
        GameFlagBase SampleData;

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

        public MySerializedType<object> _valueFilterType; //當我是ObjectVariable時，才用的到這個？

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
            bindedVariables = FindObjectsByType<AbstractMonoVariable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(v => v.varTag == this).ToArray();
            bindedVariableSetters = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IVariableTagSetter>()
                .Where(v => v.refVariableTag == this).ToArray();
        }

        [PreviewInInspector] IVariableTagSetter[] bindedVariableSetters;
#endif
    }
}