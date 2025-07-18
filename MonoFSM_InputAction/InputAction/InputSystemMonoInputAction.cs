using MonoFSM.Core.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM_InputAction
{
    //FIXME: 應該綁這個為主？DI IsPressed實作
    [RequireComponent(typeof(AbstractMonoInputAction))]
    public class InputSystemMonoInputAction : MonoBehaviour, IMonoInputAction
    {
        [PreviewInInspector] [AutoParent] private PlayerInput _localPlayerInput;
        public int InputActionId => _inputActionData.actionID; //還是monobehaviour自己assign就好？

        [SOConfig("PlayerInputActionData")] [SerializeField]
        protected InputActionData _inputActionData;
        // private InputActionMap _inputActionMap;
        public InputAction myAction =>
            _inputActionData ? _localPlayerInput?.actions[_inputActionData?.inputAction?.name] : null;
        // public InputAction myAction => _localPlayerInput.currentActionMap.FindAction(_inputActionData.inputAction.name);

        public virtual bool IsLocalPressed =>
            Application.isPlaying && (myAction.IsPressed() || myAction.WasPressedThisFrame());

        public virtual bool IsPressed => myAction.IsPressed(); //如果外掛

        public virtual bool WasPressed => myAction.WasPressedThisFrame(); //FIXME: 這個是local的


        public virtual bool WasReleased => myAction.WasReleasedThisFrame(); //FIXME: 這個是local的
        
    }
}