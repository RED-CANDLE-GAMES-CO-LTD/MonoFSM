using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using RCGMaker.Core.DataProvider;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RCGInputAction
{
    public class PlayerActionIsPressCondition : AbstractConditionComp,IFloatProvider
    {
  
        string IFloatProvider.Description => Description;

        protected override string Description => _actionRef ? _actionRef.action.name + " Is Pressed" : "No ActionRef";

        // [AutoParent] PlayerInputActionBufferManager _bufferManager;
        //FIXME: 要把was press, is press, was release分開做嗎？
        // PlayerInputActionListener _listener;
        [FormerlySerializedAs("ActionRef")] public InputActionReference _actionRef; //好像要回去找中心化的input buffer dict,
        [PreviewInInspector] [AutoParent] PlayerInput playerInput; //FIXME: 要再抽一層，做角色控制的話，直接作為ConditionComp NPC會烙賽
        private InputAction action => playerInput ? playerInput.GetAction(_actionRef) : null;
        protected override bool IsValid => action != null && action.IsPressed();
        
        [PreviewInInspector]
        public float GetFloat()
        {
            return IsValid ? 1 : 0;
        }

    }
}