using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    /// (A - B) op C，常用於時間差判斷，例如 Now - LastTriggerTime > Cooldown
    /// </summary>
    public class VarFloatDiffCompareCondition : AbstractConditionBehaviour, ITransitionCheckInvoker
    {
        public override string Description => _varA != null && _varB != null
            ? _varA.name + " - " + _varB.name + " " +
              ArithmeticHelper.OperatorDescription(_op) + " " + GetCompareValueDescription()
            : "null var";

        private string GetCompareValueDescription()
        {
            return _compareWithVariable
                ? (_targetVariable?.name ?? "null")
                : _targetValue.ToString();
        }

        private void OnVariableChanged()
        {
            Rename();
        }

        [OnValueChanged(nameof(OnVariableChanged))] [DropDownRef]
        public VarFloat _varA;

        [OnValueChanged(nameof(OnVariableChanged))] [DropDownRef]
        public VarFloat _varB;

        public Operator _op = Operator.LessThan;

        [OnValueChanged(nameof(OnVariableChanged))]
        public bool _compareWithVariable;

        [ShowIf(nameof(_compareWithVariable))]
        [OnValueChanged(nameof(OnVariableChanged))]
        [DropDownRef]
        public VarFloat _targetVariable;

        [HideIf(nameof(_compareWithVariable))]
        public float _targetValue;

        protected override bool IsValid
        {
            get
            {
                if (_varA == null || _varB == null) return false;

                var diff = _varA.Value - _varB.Value;
                var compareValue = _compareWithVariable
                    ? (_targetVariable?.Value ?? 0f)
                    : _targetValue;

                return ArithmeticHelper.CompareValues(diff, compareValue, _op);
            }
        }
    }
}
