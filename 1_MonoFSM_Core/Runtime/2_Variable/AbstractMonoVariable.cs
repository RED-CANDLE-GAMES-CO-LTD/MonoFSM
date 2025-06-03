using System;
using System.Collections.Generic;
using System.Reflection;
using jerryee.UnityMCP;
using MonoFSM.Variable.VariableBinder;
using RCGExtension;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    public abstract class AbstractMonoVariable : MonoBehaviour, IGuidEntity, IName, IValueOfKey<VariableTag>,
        IOverrideHierarchyIcon
    {
        public string IconName { get; }
        public bool IsDrawingIcon => true;
#if UNITY_EDITOR
        public Texture2D CustomIcon =>
            UnityEditor.EditorGUIUtility.ObjectContent(null, GetType()).image as Texture2D;
        //UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.rcgmaker.fsm/RCGMakerFSMCore/Runtime/2_Variable/VarFloatIcon.png");
#endif

        public UnityAction OnValueChangedRaw; //任何數值改變就通知, UI有用到很重要 //override?

        [Button]
        private void UpdateTag()
        {
            _varTag._variableType.SetType(GetType());
            _varTag._valueFilterType.SetType(ValueType);
            // Debug.Log("Tag Changed");
            //variable folder refresh
            var variableFolder = GetComponentInParent<VariableFolder>();
            if (variableFolder)
                variableFolder.Refresh();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_varTag);
#endif
        }

        [FormerlySerializedAs("varTag")]
        [MCPExtractable]
        [OnValueChanged(nameof(UpdateTag))]
        [Header("變數名稱")]
        [PropertyOrder(-1)]
        [Required]
        [SOConfig("VariableType", nameof(CreateTagPostProcess))]
        public VariableTag _varTag; //直接看當下是什麼就可以

        protected void CreateTagPostProcess()
        {
            //FIXME: 從Drawer call 失敗了，感覺varTag還沒做好...
            // varTag._variableType.SetType(GetType());
            // varTag._valueFilterType.SetType(ValueType);
            // Debug.Log("CreateTagPostProcess" + varTag._variableType.RestrictType + varTag._valueFilterType.RestrictType,
            //     varTag);
        }

        // public abstract void CommitValue();
        // public abstract void SetValue(object value, MonoBehaviour byWho = null); //一開始就預設要可以Set了
        public abstract GameFlagBase FinalData { get; } //這是啥？
        public abstract Type FinalDataType { get; }
        public abstract Type ValueType { get; }

        public abstract object objectValue { get; }

        public virtual T GetValue<T>()
        {
            var value = objectValue;
            if (value == null)
                return default;
            try
            {
                return (T)value;
            }
            catch (Exception e)
            {
                Debug.LogError($"Cannot cast {value} to {typeof(T)}", this);
                return default;
            }
        }

        protected abstract void SetValueInternal<T>(T value, Object byWho = null);

        public void SetValue<T>(T value, MonoBehaviour byWho = null)
        {
            SetValueInternal(value, byWho);
            OnValueChangedRaw?.Invoke(); //通知有人改變了
            //FIXME: 如果還有什麼需要處理的？
        }

        public object GetProperty(string knownFieldName)
        {
            return GetPropertyCache(knownFieldName)?.Invoke(this);
        }

        public Dictionary<string, Func<AbstractMonoVariable, object>> propertyCache = new();

        //GameFlagDescriptable有一樣的東西喔
        public Func<AbstractMonoVariable, object> GetPropertyCache(
            string propertyName)
        {
            if (propertyCache.TryGetValue(propertyName, out var info))
                return info;


            var propertyInfo = GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

            if (propertyInfo == null)
            {
                propertyCache[propertyName] = null;
                //FIXME: 可能因為unknownData所以有可能會找不到 有點危險？
                // Debug.LogError($"Property {propertyName} not found in {GetType()}");
                return null;
            }

            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                Debug.LogError($"Property {propertyName} does not have a getter in {GetType()}"
                );
                return null;
            }

            Func<AbstractMonoVariable, object>
                getMyProperty = (source) => getMethod.Invoke(source, null);
            propertyCache[propertyName] = getMyProperty;
            return getMyProperty;
        }

#if UNITY_EDITOR
        [Header("GameState 功能說明")] [TextArea(1, 4)]
        public string description;
#endif

        // [HideInInlineEditors] [Header("Flag Setting")]
        // public FlagTypeScriptable typeScriptable;
        protected virtual void Awake()
        {
        }

        //FIXME: virtual variable?
        // [FormerlySerializedAs("VariableSource")]
        // [ShowIf("VariableSource")] 
        // [InlineEditor] public AbstractMonoVariable VariableSource; //用別人的值 //FIXME: 什麼時候會用到這個？

        [ReadOnly] public List<AbstractVariableConsumer> consumers; //有誰有用我，binder綁一下


        //FIXME: 這個是錯的，要改成用scriptableData的 (flagFlied的？
        // public UnityEvent ValueChangedEvent => valueChangedEvent;

        // [HideInInlineEditors] public UnityEvent valueChangedEvent;
        public string Name => gameObject.name;
        public VariableTag Key => _varTag;

        public VariableTag[] GetKeys()
        {
            return new[] { _varTag };
        }
    }
}