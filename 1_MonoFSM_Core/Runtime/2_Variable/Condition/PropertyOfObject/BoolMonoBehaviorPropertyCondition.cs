using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

using Sirenix.OdinInspector;

namespace MonoFSM.Variable.Condition
{
    //選到一個任何MonoBehavior的bool property
    public class BoolMonoBehaviorPropertyCondition : AbstractFieldConditionBehaviour<bool, MonoBehaviour>
    {
        protected override bool IsValid 
            => SourceValue == TargetValue;
    }

    public abstract class AbstractFieldConditionBehaviour<TField, TSource> : AbstractConditionBehaviour
        where TSource : UnityEngine.Object
    {
        [FormerlySerializedAs("target")] public TSource sourceObject;

        private IEnumerable<string> GetBoolPropertyNames() 
            => sourceObject.GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(TField))
                .Select(p => p.Name);

        [ValueDropdown(nameof(GetBoolPropertyNames))]
        public string propertyName;

        [Header("小心 bool default 是false")] [FormerlySerializedAs("targetValue")]
        public TField TargetValue;

        public TField SourceValue => GetPropertyInfo().Invoke(); //喔喔不需要吃參數了 已經確定是sourceObject的特定property


        private Func<TField> _getMyProperty;

        protected Func<TField> GetPropertyInfo()
        {
            if (_getMyProperty != null) return _getMyProperty;

            var propertyInfo = sourceObject.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                Debug.LogError($"Property {propertyName} not found in {sourceObject.GetType()}", sourceObject);
                return null;
            }

            _getMyProperty = (Func<TField>)Delegate.CreateDelegate(typeof(Func<TField>), sourceObject,
                propertyInfo.GetGetMethod());

            return _getMyProperty;
        }
    }
}