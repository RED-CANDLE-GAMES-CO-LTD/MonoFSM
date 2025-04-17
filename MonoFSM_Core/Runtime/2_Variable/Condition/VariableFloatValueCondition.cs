using System;
using RCGMaker.Core.DataProvider;
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
    /// 和FloatCompareCondition重複？還是這個要做成簡單版？
    /// </summary>
    public class VariableFloatValueCondition : AbstractConditionComp,IResetStart,ITransitionCheckInvoker
    {
        protected override string Description => _monoVariableFloat != null
            ? name = "[Condition] " + _monoVariableFloat + " " + op + " " + targetValue
            : name = "[Condition]";

        void OnVariableChanged()
        {
            RenameOfGameObject();
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

                switch (op)
                {
                    case Operator.Equals:
                        return value == targetValue;
                    case Operator.NotEqual:
                        return value != targetValue;
                    case Operator.GreaterThan:
                        return value > targetValue;
                    case Operator.LessThan:
                        return value < targetValue;
                    case Operator.GreaterThanOrEqual:
                        return value >= targetValue;
                    case Operator.LessThanOrEqual:
                        return value <= targetValue;
                }

                return false;
            } 
        }

        //FIXME: condition本來就要實作狀態變化？必須listener? 會不會太強求？
        public void OnValueChanged(float value)
        {
            _parentTransition.IsTransitionCheckNeeded = true;
        }

        public void ResetStart()
        {
            //會和varbool 的reset 執行順序打架！
            Debug.Log("LevelResetPrepareRuntimeData", this);
            _monoVariableFloat.Field.AddListener(OnValueChanged, this);
            //需要清掉嗎？還是leveldestroy就會自己把field的listener清掉？
        }
    }
}