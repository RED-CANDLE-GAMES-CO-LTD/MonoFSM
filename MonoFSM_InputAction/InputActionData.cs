using UnityEngine;
using UnityEngine.InputSystem;


[CreateAssetMenu(menuName = "RCG/Input/InputActionData", fileName = "InputActionData", order = 0)]
public class InputActionData : ScriptableObject
{
   public InputActionReference inputAction;

   public bool WasPressed() => inputAction.action.WasPressedThisFrame();
   public bool IsPressed() => inputAction.action.IsPressed();
   public bool WasReleased() => inputAction.action.WasReleasedThisFrame();
}


