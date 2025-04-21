using System.Collections.Generic;
using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGFSM.Animation
{
    public class AnimatorParameterSetValueAction : AbstractStateAction
    {
        public enum ValueType
        {
            Bool,
            Float,
            Int
        }

        public ValueType valueType;
        public bool IsUpdateSet = false;

        [DropDownRef] //FIXME:Component?
        public Animator animator;

        [ValueDropdown(nameof(GetParameterNames))]
        public string ParameterName;

        public bool boolvalue;
        public float floatValue;
        public int intValue;

        public IFloatValueProvider floatValueSource;

        // [DropDownRef]
        // public AbstractVariable sourceVariable;
        private IEnumerable<string> GetParameterNames()
        {
            var parameters = animator.parameters;
            foreach (var parameter in parameters) yield return parameter.name;
        }

        private void SetValue()
        {
            if (floatValueSource != null)
                animator.SetFloat(ParameterName, floatValueSource.FinalValue);
            else
                switch (valueType)
                {
                    case ValueType.Bool:
                        animator.SetBool(ParameterName, boolvalue);
                        break;
                    case ValueType.Float:
                        animator.SetFloat(ParameterName, floatValue);
                        break;
                    case ValueType.Int:
                        animator.SetInteger(ParameterName, intValue);
                        break;
                }
        }

        protected override void OnStateEnterImplement()
        {
            SetValue();
        }

        protected override void OnStateUpdateImplement()
        {
            if (IsUpdateSet) SetValue();
        }
    }
}