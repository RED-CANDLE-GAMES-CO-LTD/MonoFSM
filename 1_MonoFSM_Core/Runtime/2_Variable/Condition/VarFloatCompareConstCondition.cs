using MonoFSM.Foundation;
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
    /// 拿一顆 VarFloat（_monoVariableFloat）跟常數 _targetValue 比（_op：==、!=、&gt;、&lt;、&gt;=、&lt;=）。
    /// 勾 _compareWithVariable 就改成跟另一顆 VarFloat（_targetVariable）比，常數被忽略 ——
    /// 門檻要跟別的 component 共用同一顆來源時用這個（例：發電鴿滑索的補位進度門檻）。
    /// _monoVariableFloat 沒接回 false；_targetVariable 沒接當 0 比。
    /// 專案裡已經沒有 FloatCompareCondition 這個型別了（舊註解提到的那顆）；
    /// 要比「兩顆 float 的差」用 VarFloatDiffCompareCondition（(A - B) op C），這顆只比單一值。
    /// fixme: 名字的 Const 已經名不副實（可以比變數），之後可以拿掉
    /// </summary>
    [QuickCreate(Priority = 90)] //Var 快速建立(Alt+V) 的置頂常用
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
        [FormerlySerializedAs("op")] public Operator _op = Operator.GreaterThan; //> 0最常見的組合

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

                //TODO: A-B > C?
                return ArithmeticHelper.CompareValues(value, compareValue, _op);
            }
        }

        

        // protected override IVariableField listenField => _monoVariableFloat.Field; //=
    }
}
