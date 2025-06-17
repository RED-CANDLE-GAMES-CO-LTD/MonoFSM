using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    //[Serializable]
    // public class StringHolder : IValueHolder
    // {
    //     public string Value;
    //     public object GetValue() => Value;
    // }
    // [SerializeReference, InlineProperty, ValueDropdown("GetAllowedTypes")]
    //     public IValueHolder Value;
    //FIXME: 可以用serialize reference做polymorphism嗎？ 
    public class ValueInstance<TField> : MonoBehaviour //拿值，透過某個Object的Ref 和某個property的Name, Reflection
    {
        public UnityEngine.Object sourceObject;

        private IEnumerable<string> GetPropertyNames()
        {
            return sourceObject.GetType().GetProperties().Where(p => p.PropertyType == typeof(TField))
                .Select(p => p.Name);
        }

        [ValueDropdown(nameof(GetPropertyNames))]
        public string propertyName;

        public TField SourceValue => GetPropertyInfo().Invoke(sourceObject);

        private Func<UnityEngine.Object, TField> _getMyProperty;

        private Func<UnityEngine.Object, TField> GetPropertyInfo()
        {
            if (_getMyProperty != null) return _getMyProperty;

            var propertyInfo = sourceObject.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

            if (propertyInfo == null)
            {
                Debug.LogError($"Property {propertyName} not found in {sourceObject.GetType()}", sourceObject);
                return null;
            }


            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                Debug.LogError($"Property {propertyName} does not have a getter in {sourceObject.GetType()}",
                    sourceObject);
                return null;
            }

            _getMyProperty = (source) => (TField)getMethod.Invoke(source, null);

            return _getMyProperty;
        }
    }
}