using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Variable.VariableBinder;
using RCGExtension;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.VarRefOld;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    public abstract class AbstractMonoVariable : MonoBehaviour, IGuidEntity, IName, IValueOfKey<VariableTag>,
        IOverrideHierarchyIcon, IValueProvider
    {
#if UNITY_EDITOR
        public string IconName { get; }
        public bool IsDrawingIcon => CustomIcon != null;

        public Texture2D CustomIcon =>
            UnityEditor.EditorGUIUtility.ObjectContent(null, GetType()).image as Texture2D; //雞掰！
        //UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.rcgmaker.fsm/RCGMakerFSMCore/Runtime/2_Variable/VarFloatIcon.png");
#endif

        //FIXME: 這個不能被Debug「看」，不好用... AddListener 的形式比較好
        // private UnityAction OnValueChangedRaw; //任何數值改變就通知, UI有用到很重要 //override?

        private HashSet<IVarChangedListener> _dataChangedListeners; //有誰有用我，binder綁一下
        //fuck!?

        //倒著，事件鏈超難trace
        public void OnValueChanged()
        {
            if (!Application.isPlaying)
                return;
            if (_dataChangedListeners != null)
                foreach (var item in _dataChangedListeners)
                    item.OnVarChanged(this);
            // OnValueChangedRaw?.Invoke();
            // Debug.Log("OnValueChanged", this);
        }

        public void AddListener(IVarChangedListener target)
        {
            if (_dataChangedListeners == null)
                _dataChangedListeners = new HashSet<IVarChangedListener>();
            _dataChangedListeners.Add(target);
        }

        public void RemoveListener(IVarChangedListener target)
        {
            _dataChangedListeners.Remove(target);
        }
        
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
        // [MCPExtractable]
        [OnValueChanged(nameof(UpdateTag))]
        [Header("變數名稱")]
        [PropertyOrder(-1)]
        [Required]
        [SOConfig("VariableType", nameof(CreateTagPostProcess))]
        public VariableTag _varTag; //直接看當下是什麼就可以 好像可以再往下抽？ ValueContainer? , readonly => Config, settable

        protected void CreateTagPostProcess()
        {
            //FIXME: 從Drawer call 失敗了，感覺varTag還沒做好...
            // varTag._variableType.SetType(GetType());
            // varTag._valueFilterType.SetType(ValueType);
            // Debug.Log("CreateTagPostProcess" + varTag._variableType.RestrictType + varTag._valueFilterType.RestrictType,
            //     varTag);
        }

        public T1 Get<T1>()
        {
            return GetValue<T1>();
        }

        public virtual Type ValueType => _varTag.ValueType;

        public abstract object objectValue { get; } //不好？generic value?

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

        protected abstract void SetValueInternal<T>(T value, Object byWho);

        public void SetValue<T>(T value, Object byWho)
        {
            SetValueInternal(value, byWho);
            OnValueChanged();
            // OnValueChangedRaw?.Invoke(); //通知有人改變了
            //FIXME: 如果還有什麼需要處理的？
        }

        public bool Equals(AbstractSourceValueRef sourceValueRef)
        {
            if (sourceValueRef == null)
            {
                Debug.LogError("Equals: sourceValueRef is null", this);
                return false;
            }

            var type = sourceValueRef.ValueType;
            if (type == typeof(int)) return Equals(sourceValueRef.GetValue<int>());
            if (type == typeof(float)) return Equals(sourceValueRef.GetValue<float>());
            if (type == typeof(bool)) return Equals(sourceValueRef.GetValue<bool>());
            if (type == typeof(string)) return Equals(sourceValueRef.GetValue<string>());
            if (type == typeof(Vector3)) return Equals(sourceValueRef.GetValue<Vector3>());
            if (typeof(Object).IsAssignableFrom(type)) return Equals(sourceValueRef.GetValue<Object>());
            Debug.LogWarning($"Equals: Unsupported type {type}", this);
            return Equals(sourceValueRef.GetValue<object>());
        }

        public bool Equals<T>(T value)
        {
            var v = GetValue<T>();
            return EqualityComparer<T>.Default.Equals(v, value);
        }

        public void SetValueByRef(AbstractSourceValueRef sourceValueRef, Object byWho)
        {
            if (sourceValueRef == null)
            {
                Debug.LogError("SetValue: sourceValueRef is null", this);
                return;
            }

            var type = sourceValueRef.ValueType;

            if (type == typeof(int))
            {
                SetValue(sourceValueRef.GetValue<int>(), byWho);
                return;
            }

            if (type == typeof(float))
            {
                SetValue(sourceValueRef.GetValue<float>(), byWho);
                return;
            }

            if (type == typeof(string))
            {
                SetValue(sourceValueRef.GetValue<string>(), byWho);
                return;
            }

            if (type == typeof(bool))
            {
                SetValue(sourceValueRef.GetValue<bool>(), byWho);
                return;
            }

            if (type == typeof(Vector2))
            {
                SetValue(sourceValueRef.GetValue<Vector2>(), byWho);
                return;
            }

            if (type == typeof(Vector3))
            {
                SetValue(sourceValueRef.GetValue<Vector3>(), byWho);
                return;
            }

            if (type == typeof(Vector4))
            {
                SetValue(sourceValueRef.GetValue<Vector4>(), byWho);
                return;
            }

            if (type == typeof(Quaternion))
            {
                SetValue(sourceValueRef.GetValue<Quaternion>(), byWho);
                return;
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                SetValue(sourceValueRef.GetValue<Object>(), byWho);
                return;
            }

            Debug.LogError("SetValue: Unsupported type " + type, this);
            SetValue(sourceValueRef.GetValue<object>(), byWho);
        }

        public object GetProperty(string knownFieldName)
        {
            return GetPropertyCache(knownFieldName)?.Invoke(this);
        }

        public Dictionary<string, Func<AbstractMonoVariable, object>> _propertyCache = new();

        //GameFlagDescriptable有一樣的東西喔
        public Func<AbstractMonoVariable, object> GetPropertyCache(
            string propertyName)
        {
            if (_propertyCache.TryGetValue(propertyName, out var info))
                return info;


            var propertyInfo = GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

            if (propertyInfo == null)
            {
                _propertyCache[propertyName] = null;
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
            _propertyCache[propertyName] = getMyProperty;
            return getMyProperty;
        }

#if UNITY_EDITOR
        [Header("GameState 功能說明")] [TextArea(1, 4)]
        public string description;

        public string Description
        {
            get => description;
            set => description = value;
        }
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
        public abstract bool IsValueExist { get; }

        public VariableTag[] GetKeys()
        {
            return new[] { _varTag };
        }
    }
}