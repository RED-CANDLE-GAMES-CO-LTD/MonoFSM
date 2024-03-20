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
            (bool)sourceObject.GetType().GetProperty(propertyName).GetValue(sourceObject) == TargetValue;
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

        public TField SourceValue => (TField)GetPropertyInfo().GetValue(sourceObject);

        private PropertyInfo _propertyInfo;

        private PropertyInfo GetPropertyInfo()
        {
            if (_propertyInfo == null)
                _propertyInfo = sourceObject.GetType().GetProperty(propertyName);
            return _propertyInfo;
        }
        // protected abstract bool isValid { get; }
        // protected override bool isValid =>
        //     (bool)target.GetType().GetProperty(propertyName).GetValue(target) == targetValue;
    }
}