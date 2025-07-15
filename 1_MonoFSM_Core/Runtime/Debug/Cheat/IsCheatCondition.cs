using System;
using MonoFSM.Condition;
using MonoFSM.Variable.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM.Core
{
    /// <summary>
    /// Condition that checks if a specific key is pressed.
    /// </summary>
    public class IsCheatCondition : AbstractConditionBehaviour //FIXME: parent的模組需要拔掉的話怎麼辦？
    {
        [Obsolete]
        [SerializeField] private KeyCode _keyCode;

        [SerializeField] private Key _key;
        [CompRef] [AutoParent] private IConditionChangeListener _parentConditionChangeListener;

        private bool _lastIsValid = false;
        protected override bool IsValid => _key > 0 && Keyboard.current[_key].wasPressedThisFrame;
        

        //VarStat應該不會update...怎麼監聽？需要update? IConditionUpdater? 
        private void Update()
        {
            if (IsValid == _lastIsValid) return;
            Debug.Log($"Cheat Condition Activated: {_keyCode} {IsValid}", this);
            _parentConditionChangeListener.OnConditionChanged();

            _lastIsValid = IsValid;
        }
    }
}