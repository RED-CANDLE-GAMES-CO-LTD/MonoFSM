using System;
using MonoFSM.Condition;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// Condition that checks if a specific key is pressed.
    /// </summary>
    public class IsCheatCondition : AbstractConditionComp //FIXME: parent的模組需要拔掉的話怎麼辦？
    {
        [SerializeField] private KeyCode _keyCode;
        [CompRef] [AutoParent] private IConditionChangeListener _parentConditionChangeListener;

        private bool _lastIsValid = false;
        protected override bool IsValid => Input.GetKey(_keyCode);

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