using MonoFSM_InputAction;
using Sirenix.OdinInspector;

namespace Fusion.Addons.KCC.ECM2.Examples.Networking.Fusion_v2.Characters.Scripts.Input
{
    //FIXME: 非network也可以用要怎麼兼容？ 不用AbstractFusionPlayerInput而是用interface
    //FIXME: move不能用這個
    public class InputActionWasPressedCondition : AbstractConditionBehaviour
    {
        //valid的timing怎麼處理.. networkcondition, 太難了ㄅ 只看state?
        protected override bool IsValid
        {
            get
            {
                this.Log("InputActionWasPressedCondition IsValid: " + _inputAction.WasPressed);
                return _inputAction.WasPressed;
            }
        }
        // _playerInputProvider.GetPlayerInput().WasPressed(actionData.actionID); //isvalid的timing也要小心

        // public InputActionData actionData;

        //FIXME: 用一個介面
        [DropDownRef] public AbstractMonoInputAction _inputAction;
        //resolve 去哪找？往上找
        // [AutoParent] AbstractFusionPlayerInput playerInput;
        // [AutoParent] private IPlayerInputProvider _playerInputProvider;
    }
}