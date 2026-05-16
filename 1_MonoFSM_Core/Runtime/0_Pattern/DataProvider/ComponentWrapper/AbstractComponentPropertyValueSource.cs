using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable.TypeTag;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 從任意 Component 的 property 取值的抽象 ValueSource
    /// 子類別只需指定型別 T 與要實作的 Provider 介面（IFloatProvider/IBoolProvider/IIntProvider...）
    /// 透過反射建立 Getter delegate 並 cache，避免每次 Invoke 都走反射
    /// </summary>
    public abstract class AbstractComponentPropertyValueSource<T> : AbstractValueSource<T>
    {
        [Header("類型限制")]
        [SerializeField]
        [Tooltip("限定 Component 的類型，會影響 _sourceObject dropdown 篩選")]
        [SOConfig("TypeTag")]
        private CompTypeTag _componentTypeTag;

        private Type SourceObjectTypeFilter()
        {
            if (_componentTypeTag != null && _componentTypeTag.Type != null)
                return _componentTypeTag.Type;
            return typeof(Component);
        }

        [Required]
        [DropDownRef(null, nameof(SourceObjectTypeFilter))]
        public Component _sourceObject;

        private IEnumerable<string> GetPropertyNames()
            => _sourceObject != null
                ? _sourceObject.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(T) && p.CanRead)
                    .Select(p => p.Name)
                : Enumerable.Empty<string>();

        [ValueDropdown(nameof(GetPropertyNames))]
        public string _propertyName;

        public override T Value
        {
            get
            {
                var func = GetPropertyFunc();
                return func != null ? func() : default;
            }
        }

        public Type ValueType => typeof(T);

        public override string Description =>
            _sourceObject != null && !string.IsNullOrEmpty(_propertyName)
                ? $"{_sourceObject.GetType().Name}.{_propertyName}"
                : "null";

        private Func<T> _cachedFunc;

        private Func<T> GetPropertyFunc()
        {
            if (_cachedFunc != null) return _cachedFunc;
            if (_sourceObject == null || string.IsNullOrEmpty(_propertyName)) return null;

            var propertyInfo = _sourceObject.GetType()
                .GetProperty(_propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                Debug.LogError(
                    $"Property {_propertyName} not found in {_sourceObject.GetType()}", this);
                return null;
            }

            var getter = propertyInfo.GetGetMethod();
            if (getter == null)
            {
                Debug.LogError(
                    $"Property {_propertyName} has no getter in {_sourceObject.GetType()}", this);
                return null;
            }

            _cachedFunc = (Func<T>)Delegate.CreateDelegate(
                typeof(Func<T>), _sourceObject, getter);

            return _cachedFunc;
        }
    }
}
