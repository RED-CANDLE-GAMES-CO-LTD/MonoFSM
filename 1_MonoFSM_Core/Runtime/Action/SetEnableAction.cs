using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action
{
    public class SetEnableAction : AbstractStateAction
    {
        public Behaviour _component;
        public bool _isToggle;
        [HideIf(nameof(_isToggle), false)] public bool _enable;

        protected override void OnActionExecuteImplement()
        {
            if (_isToggle)
                _component.enabled = !_component.enabled;
            else
                _component.enabled = _enable;
        }
    }
}
