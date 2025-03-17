using RCGFSMCore._0_Pattern.DataProvider.ComponentWrapper;
using RCGMaker.Core.DataProvider;
using RCGMakerFSMCore.Runtime._0_Pattern.DataProvider.ComponentWrapper;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    //分成simple和complex?
    public class VarBoolValueCondition : AbstractConditionComp,ILevelResetPrepare,ITransitionCheckInvoker
    {
        protected override string nameDescription => _monoVariableBool?.name + " == " + targetValue;
        void OnVariableChanged()
        {
            RenameOfGameObject();
        }

        
        [OnValueChanged(nameof(OnVariableChanged))] [FormerlySerializedAs("variableBool")] [Required] [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VarBool _monoVariableBool;
        //FIXME: 要用VarBoolProvider?
        [Component] [Auto] public VarBoolProviderRef _varBoolProvider;
        // [Component] [Auto] IBoolProvider _boolValue; //會再度抓到自己，...沒屁用
        public bool targetValue = true;
        //FIXME: 會有需求要比對其他東西嗎？
        protected override bool IsValid => _monoVariableBool.CurrentValue == targetValue;
        public void LevelResetPrepareRuntimeData()
        {
            _monoVariableBool.Field.AddListener(OnValueChanged, this);
            //需要清掉嗎？還是leveldestroy就會自己把field的listener清掉？
        }
        
        //FIXME: condition本來就要實作狀態變化？必須listener? 會不會太強求？
        public void OnValueChanged(bool value)
        {
            _parentTransition.IsTransitionCheckNeeded = true;
        }

    }
}