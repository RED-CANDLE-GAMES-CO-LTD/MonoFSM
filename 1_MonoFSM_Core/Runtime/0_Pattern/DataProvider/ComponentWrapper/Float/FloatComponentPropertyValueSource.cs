using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 從任意 Component 的 float property 取值的 ValueSource
    /// </summary>
    public class FloatComponentPropertyValueSource : AbstractValueSource<float>, IFloatProvider
    {
        [Required]
        public Component _sourceObject;

        private IEnumerable<string> GetFloatPropertyNames()
            => _sourceObject != null
                ? _sourceObject.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(float))
                    .Select(p => p.Name)
                : Enumerable.Empty<string>();

        [ValueDropdown(nameof(GetFloatPropertyNames))]
        public string _propertyName;

        public override float Value => GetPropertyFunc()?.Invoke() ?? 0f;

        public Type ValueType => typeof(float);

        public override string Description =>
            _sourceObject != null && !string.IsNullOrEmpty(_propertyName)
                ? $"{_sourceObject.GetType().Name}.{_propertyName}"
                : "null";

        private Func<float> _cachedFunc;

        private Func<float> GetPropertyFunc()
        {
            if (_cachedFunc != null) return _cachedFunc;
            if (_sourceObject == null || string.IsNullOrEmpty(_propertyName)) return null;

            var propertyInfo = _sourceObject.GetType()
                .GetProperty(_propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                Debug.LogError($"Property {_propertyName} not found in {_sourceObject.GetType()}", this);
                return null;
            }

            _cachedFunc = (Func<float>)Delegate.CreateDelegate(
                typeof(Func<float>), _sourceObject, propertyInfo.GetGetMethod());

            return _cachedFunc;
        }
    }
}
