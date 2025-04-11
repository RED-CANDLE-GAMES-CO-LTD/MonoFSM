using System;
using System.Collections.Generic;
using PlayerActionControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RCGInputAction
{
    public static class PlayerInputExtensions
    {
       public static InputAction GetAction(this PlayerInput playerInput, InputActionReference actionReference)
       {
           if(actionReference == null)
               return null;
           return playerInput.actions[actionReference.action.name];
       }
    }
    
    //FIXME: 用SwitchCurrentActionMapAction 代替
    public class PlayerInputActionBufferManager:MonoBehaviour
    {
        public InputActionReference toggleToUIScheme;
        public InputActionReference toggleToPlayerScheme;

        public PlayerInput playerInput;
        InputAction toggleToUISchemeAction => playerInput.GetAction(toggleToUIScheme);
        InputAction toggleToPlayerSchemeAction => playerInput.GetAction(toggleToPlayerScheme);
        //FIXME: string Variable 露出？ state machine.name?
        private void Update()
        {
            if(playerInput.currentActionMap.name == "UI")
            {
                
                if (toggleToPlayerSchemeAction.WasPressedThisFrame())
                {
                    Debug.Log("toggleToPlayerScheme");
                    playerInput.SwitchCurrentActionMap("Player");
                }
            }
            else if(playerInput.currentActionMap.name == "Player")
            {
                // Debug.Log("Player");
                if (toggleToUISchemeAction.WasPressedThisFrame())
                {
                    Debug.Log("toggleToUIScheme");
                    playerInput.SwitchCurrentActionMap("UI");
                }
            }
            else
            {
                Debug.Log("Other");
            }
        }

        //
        // [PreviewInInspector]
        // public Dictionary<InputAction,PlayerInputActionListener> dict => _actionListenerDict;
    }
}