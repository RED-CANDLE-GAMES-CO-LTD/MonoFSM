using UnityEngine.Serialization;

using Sirenix.OdinInspector;

using MonoFSM.Condition;

public enum Operator //FIXME: equality operator
{
    Equals, //==
    NotEqual, // !=
    GreaterThan, // >
    LessThan, // <
    GreaterThanOrEqual, // >=
    LessThanOrEqual // <=
}

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    /// 和FloatCompareCondition重複？還是這個要做成簡單版？ simple compare
    /// </summary>
    public class VarFloatCompareConstCondition : NotifyConditionBehaviour, ITransitionCheckInvoker
    {
        public override string Description => _monoVariableFloat != null
            ? name = _monoVariableFloat.name + " " + GetOpString() + " " + targetValue
            : name = "null var";

        private void OnVariableChanged()
        {
            Rename();
        }

        // [DropDownRef]
        // public VarFloat _monoVarFloat;
        public Operator op;

        private string GetOpString()
        {
            return op switch
            {
                Operator.Equals => "==",
                Operator.NotEqual => "!=",
                Operator.GreaterThan => ">",
                Operator.LessThan => "<",
                Operator.GreaterThanOrEqual => ">=",
                Operator.LessThanOrEqual => "<=",
                _ => ""
            };
        }

        [OnValueChanged(nameof(OnVariableChanged))] [FormerlySerializedAs("variableBool")] [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VarFloat _monoVariableFloat;

        //FIXME: 要用VarBoolProvider?
        // [Component] [Auto] public VariablefloatProviderRef _varFloatProvider;
        // [Component] [Auto] IBoolProvider _boolValue; //會再度抓到自己，...沒屁用
        public float targetValue = 0;

        //FIXME: 會有需求要比對其他東西嗎？
        protected override bool IsValid
        {
            get
            {
                var value = _monoVariableFloat.Value;

                return op switch
                {
                    Operator.Equals => value == targetValue,
                    Operator.NotEqual => value != targetValue,
                    Operator.GreaterThan => value > targetValue,
                    Operator.LessThan => value < targetValue,
                    Operator.GreaterThanOrEqual => value >= targetValue,
                    Operator.LessThanOrEqual => value <= targetValue,
                    _ => false
                };
            }
        }

        protected override IVariableField listenField => _monoVariableFloat.Field; //=
    }
}