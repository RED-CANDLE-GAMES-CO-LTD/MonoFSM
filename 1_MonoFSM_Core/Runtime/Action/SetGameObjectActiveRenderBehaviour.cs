using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action
{
    /// <summary>
    /// 每個 render frame 把 _target（與 _addTargets 裡的每一顆）SetActive 成 _active 的值。
    /// 因為是每幀覆寫而不是進場觸發一次，配上 _conditionGroup 就等於「狀態閘門」：
    /// 一顆設 true、一顆設反向條件設 false，物件就會跟著狀態自動開關，pool 回收再生也不用另外還原。
    /// _isToggle 勾起來改成每幀反轉（給閃爍用），此時 _active 被忽略。
    /// </summary>
    public class SetGameObjectActiveRenderBehaviour : AbstractRenderBehaviour
    {
        public GameObject _target;
        public GameObject[] _addTargets;
        public bool _isToggle;

        public override string Description =>
            "SetActive: " + (_target != null ? _target.name : "null") + " to " +
            (_isToggle ? "Toggle" : _active.Description);
        [HideIf(nameof(_isToggle))] [SerializeField]
        private VarBoolWrapper _active;

        public override void OnEnterRenderImplement()
        {
            bool value;
            if (_isToggle)
                value = !_target.activeSelf;
            else
                value = _active.Value;

            if (_target == null)
            {
                Debug.LogError("_target is null", this);
                return;
            }
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
