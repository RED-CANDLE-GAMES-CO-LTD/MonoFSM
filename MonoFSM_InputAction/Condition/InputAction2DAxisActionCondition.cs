using MonoFSM_InputAction;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGInputAction
{
    //FIXME:
    public class InputAction2DAxisActionCondition : AbstractConditionBehaviour
    {
        [Required]
        [DropDownRef] public MonoInputAction _inputAction;

        // [FormerlySerializedAs("ActionRef")] public InputActionReference _actionRef;
        //timing問題？
        //_inputAction?.ReadValueVec2
        [ShowInPlayMode]
        protected override bool IsValid
        {
            get
            {
                //如果local value就對了
                var result = _inputAction?.ReadValueVec2.magnitude > 0.01f;

                if (result)
                {
                    _lastValidTime = Time.time;
                    return true;
                }

                return false;
            }
        }

        [ShowInDebugMode] private float _lastValidTime;

        // [PreviewInInspector] [AutoParent] private PlayerInput playerInput;

        // private InputAction action =>
        //     playerInput != null && _actionRef != null ? playerInput.actions[_actionRef.action.name] : null;

        [ShowInDebugMode]
        public Vector2 axisValue => _inputAction?.ReadValueVec2 ?? Vector2.zero;
    }
}
