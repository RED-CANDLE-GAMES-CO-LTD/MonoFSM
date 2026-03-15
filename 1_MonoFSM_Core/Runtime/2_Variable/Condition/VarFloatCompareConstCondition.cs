using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
    public class VarFloatCompareConstCondition : AbstractConditionBehaviour, ITransitionCheckInvoker
    {
        public override string Description => _monoVariableFloat != null
            ? _monoVariableFloat.name + " " + ArithmeticHelper.OperatorDescription(_op) + " " +
              GetCompareValueDescription()
            : "null var";

        private string GetCompareValueDescription()
        {
            return _compareWithVariable
                ? (_targetVariable?.name ?? "null")
                : _targetValue.ToString();
        }

        private void OnVariableChanged()
        {
            Debug.Log("OnVariableChanged: " + _monoVariableFloat.name, this);
            Rename();
        }


        [OnValueChanged(nameof(OnVariableChanged))] [FormerlySerializedAs("variableBool")] [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VarFloat _monoVariableFloat;


        // [DropDownRef]
        // public VarFloat _monoVarFloat;
        [FormerlySerializedAs("op")] public Operator _op;

        [OnValueChanged(nameof(OnVariableChanged))]
        public bool _compareWithVariable;

        [ShowIf(nameof(_compareWithVariable))]
        [OnValueChanged(nameof(OnVariableChanged))]
        [DropDownRef]
        public VarFloat _targetVariable; //hmm這種好麻煩喔，有showIf的就很不好做validation?

        //FIXME: 怎麼做 less than half?
        [FormerlySerializedAs("targetValue")] [HideIf(nameof(_compareWithVariable))]
        public float _targetValue;

        //FIXME: 會有需求要比對其他東西嗎？
        protected override bool IsValid
        {
            get
            {
                if (_monoVariableFloat == null) return false;

                var value = _monoVariableFloat.Value;
                var compareValue = _compareWithVariable
                    ? (_targetVariable?.Value ?? 0f)
                    : _targetValue;

                return ArithmeticHelper.CompareValues(value, compareValue, _op);
            }
        }

        // protected override IVariableField listenField => _monoVariableFloat.Field; //=
    }
}
