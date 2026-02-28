using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    public class GameDataBoolCondition : AbstractConditionBehaviour
    {
        [SerializeField] private GameDataBool _gameDataBool;
        [SerializeField] private bool _expectedValue = true;

        protected override bool IsValid => _gameDataBool != null && _gameDataBool.CurrentValue == _expectedValue;

        public override string Description =>
            _gameDataBool != null
                ? $"{_gameDataBool.name} == {_expectedValue}"
                : "null GameDataBool";
    }
}
