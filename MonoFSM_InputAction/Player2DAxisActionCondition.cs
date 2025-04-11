using System.Numerics;
using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

namespace RCGInputAction
{
    public class Player2DAxisActionCondition : AbstractConditionComp, IFloatValueProvider
    {
        public InputActionReference ActionRef;
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
        InputAction action =>
            playerInput != null && ActionRef != null ? playerInput.actions[ActionRef.action.name] : null;

        [PreviewInInspector]
        public Vector2 axisValue =>
            action != null ? action.ReadValue<Vector2>() : Vector2.Zero; //ActionRef.action.ReadValue<Vector2>();

        [PreviewInInspector] public float FinalValue => action.GetControlMagnitude();
    }
}