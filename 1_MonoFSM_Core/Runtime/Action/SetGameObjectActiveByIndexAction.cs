using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action
{
    /// <summary>
    /// 依 index 多選一啟用：_targets 中只有 index == _index.Value 的會被 SetActive(true)，其餘關閉
    /// index 超出範圍時全部關閉
    /// </summary>
    public class SetGameObjectActiveByIndexAction : AbstractStateAction, IRenderBehaiour
    {
        public override string Description => "SetActive by Index: " + _index.Description;

        public GameObject[] _targets;
        [SerializeField] private VarIntWrapper _index;

        protected override void OnActionExecuteImplement()
        {
            var index = _index.Value;
            for (var i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] == null)
                    continue;
                _targets[i].SetActive(i == index);
            }
        }

        public void OnEnterRender()
        {
            OnActionExecuteImplement();
        }

        public void OnRender()
        {
            OnActionExecuteImplement();
        }
    }
}
