using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action
{
    //FIXME: 不好...會不好回查 reference?
    public class SetEnableAction : AbstractStateAction
    {
        public override string Description => "Set Enable: " + _component.GetType().Name + " to " +
                                              (_isToggle ? "Toggle" : _enable.Value.ToString());
        public Behaviour _component;
        public Behaviour[] _addcomponents;
        public bool _isToggle;

        [HideIf(nameof(_isToggle))] [SerializeField]
        private VarBoolWrapper _enable;

        protected override void OnActionExecuteImplement()
        {
            if (_isToggle)
                _component.enabled = !_component.enabled;
            else
                _component.enabled = _enable.Value;

            foreach (var comp in _addcomponents)
            {
                comp.enabled = _component.enabled;
            }
        }
    }
}
