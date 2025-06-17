using MonoFSM.Core.Attributes;
using UnityEngine.InputSystem;

namespace MonoFSM_InputAction
{
    public class InputSystemMonoInputAction : AbstractMonoInputAction
    {
        [PreviewInInspector] [AutoParent] private PlayerInput _localPlayerInput;

        // private InputActionMap _inputActionMap;
        public InputAction myAction => _localPlayerInput.actions[_inputActionData.inputAction.name];
        // public InputAction myAction => _localPlayerInput.currentActionMap.FindAction(_inputActionData.inputAction.name);

        public override bool IsLocalPressed => myAction.IsPressed() || myAction.WasPressedThisFrame();

        public override bool IsPressed => myAction.IsPressed();

        public override bool WasPressed => myAction.WasPressedThisFrame(); //FIXME: 這個是local的


        public override bool WasReleased => myAction.WasReleasedThisFrame(); //FIXME: 這個是local的
        
    }
}