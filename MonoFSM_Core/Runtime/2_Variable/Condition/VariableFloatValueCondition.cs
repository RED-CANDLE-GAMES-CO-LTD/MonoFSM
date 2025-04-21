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
    /// 和FloatCompareCondition重複？還是這個要做成簡單版？
    /// </summary>
    public class VariableFloatValueCondition : NotifyConditionComp, ITransitionCheckInvoker
    {
        protected override string Description => _monoVariableFloat != null
            ? name = "[Condition] " + _monoVariableFloat + " " + op + " " + targetValue
            : name = "[Condition]";

        private void OnVariableChanged()
        {
            Rename();
        }

        // [DropDownRef]
        // public VarFloat _monoVarFloat;
        public Operator op;


        [OnValueChanged(nameof(OnVariableChanged))] [FormerlySerializedAs("variableBool")] [Required] [DropDownRef]
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