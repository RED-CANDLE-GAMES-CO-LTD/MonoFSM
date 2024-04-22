using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    //選到一個任何MonoBehavior的bool property
    public class BoolMonoBehaviorPropertyCondition : AbstractFieldConditionComp<bool, MonoBehaviour>
    {
        protected override bool isValid =>
            SourceValue == TargetValue;
    }

    public abstract class AbstractFieldConditionComp<TField, TSource> : AbstractConditionComp
        where TSource : UnityEngine.Object
    {
        [FormerlySerializedAs("target")] public TSource sourceObject;

        private IEnumerable<string> GetBoolPropertyNames()
        {
            return sourceObject.GetType().GetProperties().Where(p => p.PropertyType == typeof(TField))
                .Select(p => p.Name);
        }

        [ValueDropdown(nameof(GetBoolPropertyNames))]
        public string propertyName;

        [Header("小心 bool default 是false")]
        [FormerlySerializedAs("targetValue")] public TField TargetValue;

        public TField SourceValue => GetPropertyInfo().Invoke(sourceObject);


        private Func<TSource, TField> _getMyProperty;

        private Func<TSource, TField> GetPropertyInfo()
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
        // protected abstract bool isValid { get; }
        // protected override bool isValid =>
        //     (bool)target.GetType().GetProperty(propertyName).GetValue(target) == targetValue;
    }
}