using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RCGInputAction
{
    //生存遊戲的話，做成開關 gameplay Action 就好，還是要保留UI的ActionMap才能點擊UI
    /// <summary>
    /// 開關 PlayerInput 的 ActionMap
    /// </summary>
    public class SwitchPlayerInputActionMapAction : AbstractStateAction
    {
        private IEnumerable<string> GetPlayerActionMapNames()
        {
            return _playerInput.actions.actionMaps.Select(x => x.name);
        }

        [ValueDropdown(nameof(GetPlayerActionMapNames))]
        public string _playerActionMap;

        [FormerlySerializedAs("playerInput")] public PlayerInput _playerInput;

        public bool enableValue;

        // [PreviewInInspector]
        // string _playerActionMapName => _playerActionMap.name;
        protected override void OnActionExecuteImplement()
        {
            if (enableValue)
                _playerInput.actions.FindActionMap(_playerActionMap).Enable();
            else
                _playerInput.actions.FindActionMap(_playerActionMap).Disable();
            // playerInput.SwitchCurrentActionMap(_playerActionMap);
        }
    }
}