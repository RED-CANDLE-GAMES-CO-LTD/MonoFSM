using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action
{
    //FIXME: 不好...會不好回查 reference?
    public class SetGameObjectActiveAction : AbstractStateAction
    {
        public override string Description => "SetActive: " + (_target != null ? _target.name : "null") + " to " +
                                              (_isToggle ? "Toggle" : _active.Value.ToString());

        public GameObject _target;
        public GameObject[] _addTargets;
        public bool _isToggle;
        [HideIf(nameof(_isToggle))]
        [SerializeField] private VarBoolWrapper _active;

        protected override void OnActionExecuteImplement()
        {
            bool value;
            if (_isToggle)
                value = !_target.activeSelf;
            else
                value = _active.Value;

            _target.SetActive(value);
            foreach (var go in _addTargets)
            {
                go.SetActive(value);
            }
        }
    }
}
