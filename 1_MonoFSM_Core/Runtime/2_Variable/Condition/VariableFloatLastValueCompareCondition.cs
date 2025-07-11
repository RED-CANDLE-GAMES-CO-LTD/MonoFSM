using Sirenix.OdinInspector;

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    /// 和上個frame的值比較
    /// </summary>
    public class VariableFloatLastValueCompareCondition : AbstractConditionBehaviour
    {
        [InfoBox("比較VariableFloat的LastValue和CalValue")]
        [DropDownRef]
        public VarFloat _monoVarFloat;

        public Operator op;

        public override string Description => _monoVarFloat
            ? name = "[Condition] " + _monoVarFloat.name + " LastValue " + op + " CurrentValue"
            : "[Condition] VariableFloatLastValueCompareCondition";

        protected override bool IsValid
        {
            get
            {
                if (_monoVarFloat == null)
                {
                    return false;
                }

                return op switch
                {
                    Operator.Equals => _monoVarFloat.CurrentValue == _monoVarFloat.LastValue,
                    Operator.NotEqual => _monoVarFloat.CurrentValue != _monoVarFloat.LastValue,
                    Operator.GreaterThan => _monoVarFloat.CurrentValue > _monoVarFloat.LastValue,
                    Operator.LessThan => _monoVarFloat.CurrentValue < _monoVarFloat.LastValue,
                    Operator.GreaterThanOrEqual => _monoVarFloat.CurrentValue >= _monoVarFloat.LastValue,
                    Operator.LessThanOrEqual => _monoVarFloat.CurrentValue <= _monoVarFloat.LastValue,
                    _ => false
                };
            }
        }
    }
}