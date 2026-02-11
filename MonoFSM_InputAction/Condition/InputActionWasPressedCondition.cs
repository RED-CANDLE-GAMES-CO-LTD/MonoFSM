using MonoFSM_InputAction;
using Sirenix.OdinInspector;

namespace Fusion.Addons.KCC.ECM2.Examples.Networking.Fusion_v2.Characters.Scripts.Input
{
    //FIXME: move不能用這個
    public class InputActionWasPressedCondition : AbstractConditionBehaviour
    {
        public override string Description => $"{inputAction?.name} {_inputActionType}";

        public enum InputActionType
        {
            WasPressed,
            IsPressed,
            WasReleased,
        }

        public InputActionType _inputActionType = InputActionType.WasPressed;

        //valid的timing怎麼處理.. networkcondition, 太難了ㄅ 只看state?
        protected override bool IsValid
        {
            get
            {
                if (inputAction == null)
                    return false;
                switch (_inputActionType)
                {
                    case InputActionType.WasPressed:
                        this.Log(
                            "InputActionWasPressedCondition IsValid: ",
                            inputAction.WasPressed
                        );
                        return inputAction.WasPressed;
                    case InputActionType.IsPressed:
                        this.Log(
                            "InputActionWasPressedCondition IsValid: ",
                            inputAction.IsPressed
                        );
                        return inputAction.IsPressed;
                    case InputActionType.WasReleased:
                        this.Log(
                            "InputActionWasPressedCondition IsValid: ",
                            inputAction.WasReleased
                        );
                        return inputAction.WasReleased;
                    default:
                        return false;
                }
            }
        }

        // _playerInputProvider.GetPlayerInput().WasPressed(actionData.actionID); //isvalid的timing也要小心

        // public InputActionData actionData;

        //FIXME: 用一個介面
        [HideIf("_inputActionVar")]
        [DropDownRef]
        public MonoInputAction _inputAction;

        public MonoInputAction inputAction =>
            _inputActionVar != null ? _inputActionVar._inputActionRef : _inputAction;

        // [DropDownRef]
        public VarInputAction _inputActionVar;
        //可以再過一層？

        //resolve 去哪找？往上找
        // [AutoParent] AbstractFusionPlayerInput playerInput;
        // [AutoParent] private IPlayerInputProvider _playerInputProvider;
    }
}
