using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RCGInputAction
{
    public static class PlayerInputExtensions
    {
        public static InputAction GetAction(this PlayerInput playerInput, InputActionReference actionReference)
        {
            if (actionReference == null)
                return null;
            return playerInput.actions[actionReference.action.name];
        }
    }

    //FIXME: 用SwitchCurrentActionMapAction 代替
    //FIXME: 在寫啥？
    [Obsolete]
    public class PlayerInputActionBufferManager : MonoBehaviour
    {
        public InputActionReference toggleToUIScheme;
        public InputActionReference toggleToPlayerScheme;

        public PlayerInput playerInput;
        InputAction toggleToUISchemeAction => playerInput.GetAction(toggleToUIScheme);
        InputAction toggleToPlayerSchemeAction => playerInput.GetAction(toggleToPlayerScheme);
        //FIXME: string Variable 露出？ state machine.name?
        private void Update()
        {
            switch (playerInput.currentActionMap.name)
            {
                case "UI":
                    {
                        if (toggleToPlayerSchemeAction.WasPressedThisFrame())
                        {
                            Debug.Log("toggleToPlayerScheme");
                            playerInput.SwitchCurrentActionMap("Player");
                        }

                        break;
                    }
                case "Player":
                    {
                        // Debug.Log("Player");
                        if (toggleToUISchemeAction.WasPressedThisFrame())
                        {
                            Debug.Log("toggleToUIScheme");
                            playerInput.SwitchCurrentActionMap("UI");
                        }

                        break;
                    }
                default:
                    Debug.Log("Other");
                    break;
            }
        }
    }
}