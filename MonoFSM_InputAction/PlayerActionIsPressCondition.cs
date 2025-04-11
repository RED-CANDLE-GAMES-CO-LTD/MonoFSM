using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using UnityEngine.InputSystem;

namespace RCGInputAction
{
    public class PlayerActionIsPressCondition : AbstractConditionComp, IFloatValueProvider
    {
        protected override string Description => ActionRef ? ActionRef.action.name + " Is Pressed" : "No ActionRef";

        // [AutoParent] PlayerInputActionBufferManager _bufferManager;
        //FIXME: 要把was press, is press, was release分開做嗎？
        // PlayerInputActionListener _listener;
        public InputActionReference ActionRef; //好像要回去找中心化的input buffer dict,
        [PreviewInInspector] [AutoParent] PlayerInput playerInput; //FIXME: 要再抽一層，做角色控制的話，直接作為ConditionComp NPC會烙賽
        private InputAction action => playerInput ? playerInput.GetAction(ActionRef) : null;
        protected override bool IsValid => action != null && action.IsPressed();

        [PreviewInInspector] public float FinalValue => IsValid ? 1 : 0;
    }
}