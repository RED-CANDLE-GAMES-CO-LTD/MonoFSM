using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

namespace RCGInputAction
{
    
    //生存遊戲的話，做成開關 gameplay Action 就好，還是要保留UI的ActionMap才能點擊UI
    public class SwitchPlayerInputActionMapAction:AbstractStateAction
    {
        IEnumerable<string> GetPlayerActionMapNames()
        {
            return playerInput.actions.actionMaps.Select(x => x.name);
        }
        
        [ValueDropdown(nameof(GetPlayerActionMapNames))]
        public string _playerActionMap;
        public PlayerInput playerInput;

        public bool enableValue;
        // [PreviewInInspector]
        // string _playerActionMapName => _playerActionMap.name;
        protected override void OnStateEnterImplement()
        {
            if(enableValue)
                playerInput.actions.FindActionMap(_playerActionMap).Enable();
            else
                playerInput.actions.FindActionMap(_playerActionMap).Disable();
            // playerInput.SwitchCurrentActionMap(_playerActionMap);
        }
        
        
    }
}