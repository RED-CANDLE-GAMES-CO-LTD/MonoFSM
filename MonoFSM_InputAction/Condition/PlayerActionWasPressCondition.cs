using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerActionControl
{
    //GameplayActionWasPressCondition
    //有buffering
    public class PlayerActionWasPressCondition : AbstractConditionBehaviour
    {
        public override string Description =>
            actionOfRef != null ? actionOfRef.name + "Was Press Buffer" : "No ActionRef";

        // [AutoParent] PlayerInputActionBufferManager _bufferManager;
        //FIXME: 要把was press, is press, was release分開做嗎？
        // PlayerInputActionListener _listener;
        [HideIf(nameof(ActionDriver))] public InputActionReference ActionRef;

        private InputAction actionOfRef
        {
            get
            {
                if (ActionDriver != null)
                    return ActionDriver._actionRef.action;
                if (ActionRef != null)
                    return ActionRef.action;
                return null;
            }
        }

        [DropDownRef(_parentType = typeof(PlayerInput))]
        public PlayerBufferedInputAction ActionDriver;

        [PreviewInInspector] [AutoParent] PlayerInput playerInput; //FIXME: 要再抽一層，做角色控制的話，直接作為ConditionComp NPC會烙賽

        //從playerInput拿到action才是這個device的action
        private InputAction action => playerInput != null ? playerInput.actions[actionOfRef.name] : null;

        protected override bool IsValid =>
            action != null && PlayerBufferedInputAction.GetListener(action).WasPressBuffered();

        public float FinalValue => IsValid ? 1 : 0;
    }
}