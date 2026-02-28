using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.Action.AnimatorActions;
using MonoDebugSetting;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

// using MonoFSM.Core.Runtime.Conditions;

namespace MonoFSM.Animation
{
    /// <summary>
    ///     實驗性，還要整理
    /// </summary>
    public class AnimatorParameterSetValueAction : AbstractStateAction, ISceneStart
    {
        public enum ValueType
        {
            Bool,
            Float,
            Int,
        }

        public override string Description =>
            $"Set Animator Parameter {ParameterName} to {valueType} Value";

        public ValueType valueType;
        public bool IsUpdateSet = false;

        [HideIf(nameof(_animatorRefSource))]
        [TitleGroup("Animator")]
        [BoxGroup("Animator/Animator")]
        [Required]
        [DropDownRef]
        [FormerlySerializedAs("animator")]
        public Animator _animator;

        [ShowInInspector]
        private Animator animator =>
            _animatorRefSource != null ? _animatorRefSource.Value : _animator;

        [FormerlySerializedAs("_animatorRefProvider")]
        [TitleGroup("Animator")]
        [BoxGroup("Animator/Animator")]
        [SerializeField]
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private AnimatorRefSource _animatorRefSource;

        [ValueDropdown(nameof(GetParameterNames))]
        public string ParameterName;

        //每種datatype拆開？
        [SerializeField]
        private float _interpolate;

        //invert bool value?
        // public bool _boolValue;

        private bool _hasParameter;
        private bool _hasCheckedParameter;

        #region Float

        private float _lastValue;

        [HideIf(nameof(_varFloat))]
        public float _floatValue;

        [SerializeField]
        private VarFloat _varFloat;

//varfloatWrapper?
        private float floatValue => _varFloat != null ? _varFloat.Value : _floatValue;
        #endregion

        public int _intValue;

        [SerializeField]
        private VarInt _varInt;
        int intValue => _varInt != null ? _varInt.Value : _intValue;

        // [Auto]
        // [PreviewInInspector]
        // public IFloatProvider _floatValueSource;

        // [DropDownRef]
        // public AbstractVariable sourceVariable;
        private IEnumerable<string> GetParameterNames()
        {
            var parameters = animator.parameters;
            foreach (var parameter in parameters)
                yield return parameter.name;
        }

        private bool HasParameter(string paramName)
        {
            if (_hasCheckedParameter)
                return _hasParameter;

            _hasCheckedParameter = true;
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName)
                {
                    _hasParameter = true;
                    return true;
                }
            }

            _hasParameter = false;
            return false;
        }

        private void SetValue()
        {
            if (!animator.isActiveAndEnabled)
                return;

            if (!HasParameter(ParameterName))
            {
                Debug.LogWarning(
                    $"Animator parameter '{ParameterName}' does not exist on {animator.gameObject.name}",
                    this);
                return;
            }
            else
            {
                switch (valueType)
                {
                    case ValueType.Bool:
                        animator.SetBool(ParameterName,
                            IsValid); //直接對著valid做不是很爽？ FIXME: 要拆出去？ bool value吧
                        break;
                    case ValueType.Float:
                        animator.SetFloat(ParameterName, floatValue);
                        break;
                    case ValueType.Int:
                        animator.SetInteger(ParameterName, intValue);
                        break;
                }
            }
        }

        protected override void OnActionExecuteImplement()
        {
            SetValue();
        }

        protected override bool ForceExecuteInValid => true;

        //FIXME: 拔掉！
        // protected override void OnStateUpdateImplement()
        // {
        //     if (IsUpdateSet) SetValue();
        // }
        public void EnterSceneStart()
        {
            if (animator == null && gameObject.activeSelf)
            {
                if (RuntimeDebugSetting.IsDebugMode)
                    Debug.LogError("Animator is null", this);
                gameObject.SetActive(false);
            }
        }
    }
}
