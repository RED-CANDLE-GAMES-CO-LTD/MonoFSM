using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM_InputAction;

namespace RCGInputAction
{
    /// <summary>
    /// 消費指定 MonoInputAction 的 buffer press，防止同一次 press 重複觸發。
    /// 放在 State 的 OnEnter action 中使用。
    /// FIXME: 這也是 input 類，要把 input相關action 獨立出來嗎？讓EventHandler獨立判定觸發
    /// </summary>
    public class ConsumeInputPressAction : AbstractStateAction
    {
        public override string Description => _inputAction != null
            ? $"Consume Press of [{_inputAction.name}]"
            : "Consume Press of [null]";

        [DropDownRef] public MonoInputAction _inputAction;

        protected override void OnActionExecuteImplement()
        {
            if (_inputAction != null)
                _inputAction.ConsumePress();
        }
    }
}
