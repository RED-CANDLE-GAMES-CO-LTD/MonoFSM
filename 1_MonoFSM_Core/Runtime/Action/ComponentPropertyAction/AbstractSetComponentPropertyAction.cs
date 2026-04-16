using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.ComponentPropertyAction
{
    /// <summary>
    /// 將值 Set 到任意 Component 的 property 上的抽象 Action
    /// 子類別只需指定型別 T、提供 source value 與 description 即可
    /// 透過反射建立 Setter delegate 並 cache，避免每次 Invoke 都走反射
    /// </summary>
    public abstract class AbstractSetComponentPropertyAction<T> : AbstractStateAction
    {
        [Required] [SerializeField] protected Component _targetObject;

        private IEnumerable<string> GetPropertyNames()
            => _targetObject != null
                ? _targetObject.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(T) && p.CanWrite)
                    .Select(p => p.Name)
                : Enumerable.Empty<string>();

        [ValueDropdown(nameof(GetPropertyNames))] [SerializeField]
        protected string _propertyName;

        private Action<T> _cachedSetter;
        private Func<T> _cachedGetter;

        private PropertyInfo GetPropertyInfo()
        {
            if (_targetObject == null || string.IsNullOrEmpty(_propertyName)) return null;
            var propertyInfo = _targetObject.GetType()
                .GetProperty(_propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
                Debug.LogError(
                    $"Property {_propertyName} not found in {_targetObject.GetType()}", this);
            return propertyInfo;
        }

        private Action<T> GetSetterFunc()
        {
            if (_cachedSetter != null) return _cachedSetter;
            var propertyInfo = GetPropertyInfo();
            if (propertyInfo == null) return null;

            var setter = propertyInfo.GetSetMethod();
            if (setter == null)
            {
                Debug.LogError(
                    $"Property {_propertyName} has no setter in {_targetObject.GetType()}", this);
                return null;
            }

            _cachedSetter = (Action<T>)Delegate.CreateDelegate(
                typeof(Action<T>), _targetObject, setter);

            return _cachedSetter;
        }

        private Func<T> GetGetterFunc()
        {
            if (_cachedGetter != null) return _cachedGetter;
            var propertyInfo = GetPropertyInfo();
            if (propertyInfo == null) return null;

            var getter = propertyInfo.GetGetMethod();
            if (getter == null)
            {
                Debug.LogError(
                    $"Property {_propertyName} has no getter in {_targetObject.GetType()}", this);
                return null;
            }

            _cachedGetter = (Func<T>)Delegate.CreateDelegate(
                typeof(Func<T>), _targetObject, getter);

            return _cachedGetter;
        }

        protected abstract T GetSourceValue();
        protected abstract void SetSourceValue(T value);
        protected abstract string GetSourceDescription();

        protected override void OnActionExecuteImplement()
        {
            var setter = GetSetterFunc();
            if (setter == null) return;
            setter(GetSourceValue());

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_targetObject);
#endif
        }

        private bool CanCopyCurrentValue =>
            _targetObject != null && !string.IsNullOrEmpty(_propertyName);

        [Button("Copy Current Value → Source")]
        [ShowIf(nameof(CanCopyCurrentValue))]
        private void CopyCurrentValueToSource()
        {
            var getter = GetGetterFunc();
            if (getter == null) return;
            var current = getter();
            SetSourceValue(current);
            Debug.Log(
                $"Copy current value [{current}] of {_targetObject.GetType().Name}.{_propertyName} to source",
                this);
        }

        public override string Description =>
            _targetObject != null && !string.IsNullOrEmpty(_propertyName)
                ? $"Set {_targetObject.GetType().Name}.{_propertyName} = {GetSourceDescription()}"
                : "null";
    }
}
