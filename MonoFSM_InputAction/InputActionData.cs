using UnityEngine;
using UnityEngine.InputSystem;


[CreateAssetMenu(menuName = "RCG/Input/InputActionData", fileName = "InputActionData", order = 0)]
public class InputActionData : ScriptableObject
{
   public InputActionReference inputAction;
   public int actionID; //enum mapping for network, 自動mapping
   //local 多人是錯的
   public bool WasPressed() => inputAction.action.WasPressedThisFrame();
   public bool IsPressed() => inputAction.action.IsPressed();
   public bool WasReleased() => inputAction.action.WasReleasedThisFrame();
}


