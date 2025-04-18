using jerryee.UnityMCP;
using MonoFSM.Condition;
using MonoFSM.Variable;
using MonoFSM.DataProvider;
using RCGMaker.Core.DataProvider;
using RCGMakerFSMCore.Runtime._0_Pattern.DataProvider.ComponentWrapper;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Variable.Condition
{
    public class VarBoolValueCondition : NotifyConditionComp
    {
        protected override string Description => _varBool?.name + " == " + targetValue;

        /// <summary>
        /// Invoked when the bound variable changes.
        /// </summary>
        private void OnVariableChanged()
        {
            Rename();
        }


        [FormerlySerializedAs("_monoVariableBool")]
        [MCPExtractable]
        [OnValueChanged(nameof(OnVariableChanged))]
        [FormerlySerializedAs("variableBool")]
        [Required]
        [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VarBool _varBool;

        //FIXME: 要用VarBoolProvider?
        // [Component] [Auto] public VarBoolProviderRef _varBoolProvider;

        // [Component] [Auto] IBoolProvider _boolValue; //會再度抓到自己，...沒屁用
        public bool targetValue = true;

        //FIXME: 會有需求要比對其他東西嗎？
        protected override IVariableField listenField => _varBool.Field;
        protected override bool IsValid => _varBool.CurrentValue == targetValue;

        //FIXME: condition本來就要實作狀態變化？必須listener? 會不會太強求？
        // private void OnValueChanged(bool value)
        // {
        //     if (_parentConditionChangeListener == null)
        //         // Debug.LogError("VarBoolValueCondition: No parent transition found", this);
        //         return;
        //     _parentConditionChangeListener.OnConditionChanged();
        // }

        // public void ResetStart()
        // {
        //     Debug.Log("LevelResetPrepareRuntimeData", this);
        //     if (_parentConditionChangeListener == null)
        //         return;
        //     _monoVariableBool.Field.AddListener(OnValueChanged, this);
        //     //需要清掉嗎？還是leveldestroy就會自己把field的listener清掉？
        // }
    }
}