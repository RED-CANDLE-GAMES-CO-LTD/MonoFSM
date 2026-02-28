using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    public class GameDataFloatCondition : AbstractConditionBehaviour
    {
        [SerializeField] private GameDataFloat _gameDataFloat;
        [SerializeField] private Operator _op;
        [SerializeField] private float _targetValue;

        protected override bool IsValid =>
            _gameDataFloat != null && ArithmeticHelper.CompareValues(_gameDataFloat.Value, _targetValue, _op);

        public override string Description =>
            _gameDataFloat != null
                ? $"{_gameDataFloat.name} {ArithmeticHelper.OperatorDescription(_op)} {_targetValue}"
                : "null GameDataFloat";
    }
}
