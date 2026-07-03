using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action
{
    public class SetGameObjectActiveRenderBehaviour : AbstractRenderBehaviour
    {
        public GameObject _target;
        public GameObject[] _addTargets;
        public bool _isToggle;

        [HideIf(nameof(_isToggle))] [SerializeField]
        private VarBoolWrapper _active;

        public override void OnEnterRenderImplement()
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

        public override void OnRenderImplement()
        {
            OnEnterRenderImplement();
        }
    }
}
