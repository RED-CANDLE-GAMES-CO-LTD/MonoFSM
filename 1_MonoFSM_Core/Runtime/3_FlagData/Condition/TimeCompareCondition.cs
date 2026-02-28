using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    public class TimeCompareCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        [SerializeField] private VarFloat _currentTime;

        [ShowInInspector] [ReadOnly]
        private string CurrentTimeDisplay => _currentTime != null
            ? TimeOfDay.FromFloat(_currentTime.Value).ToString()
            : "--:--";

        [SerializeField] private Operator _op;

        [SerializeField] private TimeOfDay _targetTime;

        protected override bool IsValid =>
            _currentTime != null && ArithmeticHelper.CompareValues(_currentTime.Value, _targetTime.ToFloat(), _op);

        public override string Description =>
            _currentTime != null
                ? $"{_currentTime.name} {ArithmeticHelper.OperatorDescription(_op)} {_targetTime}"
                : "null VarFloat";
    }
}
