using System.Collections.Generic;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Animation
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

        //每種datatype拆開？
        [SerializeField] private float interpolate = 0; 
        
        
        public bool boolvalue;

        #region Float

        private float _lastValue = 0;
        public float floatValue;

        #endregion
        
        
        public int intValue;

        [Auto] [PreviewInInspector] public IFloatProvider _floatValueSource;

        // [DropDownRef]
        // public AbstractVariable sourceVariable;
        private IEnumerable<string> GetParameterNames()
        {
            var parameters = animator.parameters;
            foreach (var parameter in parameters) yield return parameter.name;
        }

        private void SetValue()
        {
            if (_floatValueSource != null)
            {
                if (interpolate == 0)
                    _lastValue = _floatValueSource.Value;
                else
                    _lastValue = Mathf.MoveTowards(_lastValue, _floatValueSource.Value,
                        interpolate * bindingState.DeltaTime); //FIXME: 不一定會有bindingState? 還是乾脆拿logic的就好了？

                animator.SetFloat(ParameterName, _lastValue);
            }
                
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

        protected override void OnActionExecuteImplement()
        {
            
            SetValue();
        }



        //FIXME: 拔掉！
        // protected override void OnStateUpdateImplement()
        // {
        //     if (IsUpdateSet) SetValue();
        // }
    }
}