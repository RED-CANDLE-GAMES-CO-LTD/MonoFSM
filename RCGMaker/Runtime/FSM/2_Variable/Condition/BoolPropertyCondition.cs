using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    //選到一個bool property
    public class BoolPropertyCondition : AbstractFieldConditionComp<bool, MonoBehaviour>
    {
        protected override bool isValid =>
            SourceValue == TargetValue;
    }

    public abstract class AbstractFieldConditionComp<TField, TSource> : AbstractConditionComp
    {
        [FormerlySerializedAs("target")] public TSource sourceObject;

        private IEnumerable<string> GetBoolPropertyNames()
        {
            return sourceObject.GetType().GetProperties().Where(p => p.PropertyType == typeof(TField))
                .Select(p => p.Name);
        }

        [ValueDropdown(nameof(GetBoolPropertyNames))]
        public string propertyName;

        [FormerlySerializedAs("targetValue")] public TField TargetValue;

        public TField SourceValue => GetPropertyInfo().Invoke(sourceObject);


        private Func<TSource, TField> _getMyProperty;

        private Func<TSource, TField> GetPropertyInfo()
        {
            if (_getMyProperty != null) return _getMyProperty;

            var propertyInfo = sourceObject.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                Debug.LogError($"Property {propertyName} not found in {sourceObject.GetType()}");
                return null;
            }

            _getMyProperty = (Func<TSource, TField>)Delegate.CreateDelegate(typeof(Func<TSource, TField>),
                propertyInfo.GetGetMethod());

            return _getMyProperty;
        }
        // protected abstract bool isValid { get; }
        // protected override bool isValid =>
        //     (bool)target.GetType().GetProperty(propertyName).GetValue(target) == targetValue;
    }
}