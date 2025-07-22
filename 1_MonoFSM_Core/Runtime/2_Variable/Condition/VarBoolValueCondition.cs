using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using MonoFSM.Condition;

// using jerryee.UnityMCP;

namespace MonoFSM.Variable.Condition
{
    public class VarBoolValueCondition : NotifyConditionBehaviour
    {
        public override string Description => _varBool?.name + " == " + targetValue;

        /// <summary>
        /// Invoked when the bound variable changes.
        /// </summary>
        private void OnVariableChanged()
        {
            Rename();
        }


        [FormerlySerializedAs("_monoVariableBool")]
        // [MCPExtractable]
        [OnValueChanged(nameof(OnVariableChanged))]
        [FormerlySerializedAs("variableBool")]
        [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VarBool _varBool;

        //FIXME: 要用VarBoolProvider?
        // [Component] [Auto] public VarBoolProviderRef _varBoolProvider;

        public override void CheatComplete()
        {
            base.CheatComplete();
            _varBool.SetValue(targetValue, this);
        }

        // [Component] [Auto] IBoolProvider _boolValue; //會再度抓到自己，...沒屁用
        public bool targetValue = true;

        //FIXME: 會有需求要比對其他東西嗎？
        protected override IVariableField listenField => _varBool.Field;
        protected override bool IsValid => _varBool.CurrentValue == targetValue;
    }
}