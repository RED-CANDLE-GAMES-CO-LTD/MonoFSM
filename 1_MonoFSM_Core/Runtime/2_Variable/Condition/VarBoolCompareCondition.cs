using MonoFSM.Condition;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

// using jerryee.UnityMCP;

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    /// 比對一顆 VarBool 的值是否等於 targetValue（預設 true）。
    /// 最常用的布林條件：掛在 [If] 節點上把關 action / transition / getter。
    /// _varBool 為 null 或該 VarBool 所在物件被停用時，條件視為不成立。
    /// </summary>
    public class VarBoolCompareCondition : AbstractConditionBehaviour
    {
        [ConditionPreset("Bool == true", Category = "Bool", Priority = 90, ColorHex = "#88D070")]
        private static void Preset_True(VarBoolCompareCondition c)
        {
            c.targetValue = true;
        }

        [ConditionPreset("Bool == false", Category = "Bool", Priority = 90, ColorHex = "#E07070")]
        private static void Preset_False(VarBoolCompareCondition c)
        {
            c.targetValue = false;
        }


        public override string Description => _varBool?.PathName + " == " + targetValue;

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
        // protected override IVariableField listenField => _varBool.Field;
        protected override bool IsValid =>
            (_varBool?.isActiveAndEnabled ?? false) && _varBool?.CurrentValue == targetValue;
        //FIXME: 要判斷有沒有開著嗎？
    }
}
