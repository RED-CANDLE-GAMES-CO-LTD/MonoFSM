using System.Numerics;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RCGInputAction
{
    public class Player2DAxisActionCondition : AbstractConditionBehaviour
    {
        [FormerlySerializedAs("ActionRef")] public InputActionReference _actionRef;
        protected override bool IsValid => action is { inProgress: true };

        [PreviewInInspector] [AutoParent] private PlayerInput playerInput;

        // [Button]
        // void GetValue()
        // {
        //     InputAction action = playerInput.actions[ActionRef.action.name];
        //     // var action2 = ActionRef.action;
        //     
        //     ActionRef.action.ReadValue<Vector2>();
        // }
        private InputAction action =>
            playerInput != null && _actionRef != null ? playerInput.actions[_actionRef.action.name] : null;

        [PreviewInInspector]
        public Vector2 axisValue =>
            action?.ReadValue<Vector2>() ?? Vector2.Zero; //ActionRef.action.ReadValue<Vector2>();

        // [PreviewInInspector] public float FinalValue => action.GetControlMagnitude();
    }
}