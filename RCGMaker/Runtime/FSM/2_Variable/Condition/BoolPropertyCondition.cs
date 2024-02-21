using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class BoolPropertyCondition : AbstractConditionComp
    {
        public MonoBehaviour target;

        private IEnumerable<string> GetBoolPropertyNames()
        {
            return target.GetType().GetProperties().Where(p => p.PropertyType == typeof(bool)).Select(p => p.Name);
        }

        [ValueDropdown(nameof(GetBoolPropertyNames))]
        public string propertyName;

        public bool targetValue;

        protected override bool isValid =>
            (bool)target.GetType().GetProperty(propertyName).GetValue(target) == targetValue;
    }
}