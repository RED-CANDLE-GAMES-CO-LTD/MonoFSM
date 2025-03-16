using RCGFSMCore._0_Pattern.DataProvider.ComponentWrapper;
using RCGMaker.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class VarBoolValueCondition : AbstractConditionComp,ILevelResetPrepare,ITransitionCheckInvoker
    {
        protected override string nameDescription => _monoVariableBool.name + " == " + targetValue;
        //FIXME: 好像可以再簡化喔
        void OnVariableChanged()
        {
            RenameOfGameObject();
        }

        //FIXME: 要用VarBoolProvider?
        [OnValueChanged(nameof(OnVariableChanged))] [FormerlySerializedAs("variableBool")] [Required] [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VarBool _monoVariableBool;
        [Component] [Auto] public VarFloatProviderRef _variableBoolProvider;
        [Component] [Auto] IBoolProvider _boolValue;
        public bool targetValue = true;
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