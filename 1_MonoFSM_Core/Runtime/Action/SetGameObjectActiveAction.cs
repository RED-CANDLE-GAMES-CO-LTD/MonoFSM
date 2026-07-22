using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action
{
    //FIXME: 不好...會不好回查 reference?
    public class SetGameObjectActiveAction : AbstractStateAction, IRenderBehaiour
    {
        protected override void Awake()
        {
            base.Awake();
            if (_target == null)
            {
                Debug.LogError("No target defined", this);
            }
        }

        public override string Description => "SetActive: " + (_target != null ? _target.name : "null") + " to " +
                                              (_isToggle ? "Toggle" : _active.Description);

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
            if (_target == null)
            {
                Debug.LogError("No target defined", this);
                return;
            }

            _target.SetActive(value);
            foreach (var go in _addTargets)
            {
                go.SetActive(value);
            }
        }

        /// <summary>
        /// 有點髒，但好像不能說錯？還是應該把SFX類的獨立出來 (但在做一樣的事)
        /// FIXME: 應該要獨立
        /// </summary>
        public void OnEnterRender() //這樣好嗎？
        {
            OnActionExecuteImplement();
        }

        public void OnRender()
        {
            OnActionExecuteImplement();
        }
    }
}
