using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_InputAction.Condition
{
    /// <summary>
    /// 「某顆按鍵已經按住超過 N 秒」的條件（長按判斷）。
    /// input 來源可直接指 MonoInputAction（_inputAction），
    /// 或透過 VarMonoInput（_monoInput）間接取得 —— 跨 entity 讀別人的輸入時用後者
    /// （例：機台用 Operator.d_Interact Input 讀操作者的 E 鍵）。
    /// 兩個都填時以 _monoInput 優先。_pressDuration <= 0 一律不成立。
    /// </summary>
    public class InputActionPressTimeCondition : AbstractConditionBehaviour
    {
        [HideIf("_monoInput")]
        [DropDownRef]
        public MonoInputAction _inputAction;

        //跨 entity 拿別人的 input：VarMonoInput 有填就用它，沒填才退回 _inputAction
        [HideIf("_inputAction")]
        [DropDownRef]
        [SerializeField]
        private VarMonoInput _monoInput;

        public MonoInputAction inputAction =>
            _monoInput != null ? _monoInput.Value : _inputAction;

        [ShowInInspector] private float pressingTime => inputAction?.PressTime ?? 0;

        [InlineField]
        [SerializeField]
        VarFloatFoldOut _pressDuration = new VarFloatFoldOut();

        protected override bool IsValid =>
            _pressDuration.Value > 0 && inputAction?.PressTime >= _pressDuration.Value;

        public override string Description
        {
            get
            {
                var inputName = _monoInput != null ? _monoInput.name : _inputAction?.name;
                return inputName != null
                    ? $"{inputName} Pressed for {_pressDuration.Value} seconds"
                    : "No Input Action";
            }
        }
    }
}
