using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    /// 每隔 _interval 秒輪替到「下一個符合條件」的 SwitchCase 並循環。
    /// 子物件結構與 SwitchCaseActionSimulator 相同（直接子層放 SwitchCase）。
    /// 某輪沒有任何 case 符合條件時，維持目前顯示，下個 interval 再試。
    /// </summary>
    public class SwitchCaseRoundRobinSimulator : AbstractDescriptionBehaviour, IUpdateSimulate,
        IRenderUpdate
    {
        protected override string DescriptionTag => "Switch RoundRobin";

        public override string Description => $"RoundRobin every {_interval.Value}s";

        [SerializeField] private VarFloatWrapper _interval = new(1f);

        [AutoChildren(DepthOneOnly = true)] [CompRef]
        private SwitchCase[] _cases;

        [ShowInInspector] private int _currentIndex = -1;
        [ShowInInspector] private float _timer;
        private bool _enterRenderPending;

        public void Simulate(float deltaTime)
        {
            if (_cases == null || _cases.Length == 0)
                return;

            _timer += deltaTime;
            if (_currentIndex >= 0 && _timer < _interval.Value)
                return;

            _timer = 0f;
            AdvanceToNextMatchedCase();
        }

        private void AdvanceToNextMatchedCase()
        {
            var count = _cases.Length;
            for (var i = 1; i <= count; i++)
            {
                var index = (_currentIndex + i) % count;
                var switchCase = _cases[index];
                if (switchCase == null || !switchCase.gameObject.activeSelf)
                    continue;
                if (switchCase.IsDefault)
                    continue;
                if (!switchCase.IsConditionMet)
                    continue;

                _currentIndex = index;
                switchCase.ExecuteActions();
                _enterRenderPending = true;
                Debug.Log($"[SwitchCaseRoundRobin] 輪到 case: {switchCase.name} (index {index})", this);
                return;
            }
        }

        public void Render(float runnerLocalRenderTime)
        {
            if (_cases == null || _currentIndex < 0 || _currentIndex >= _cases.Length)
                return;

            var current = _cases[_currentIndex];
            if (current == null || !current.gameObject.activeSelf)
                return;

            if (_enterRenderPending)
            {
                _enterRenderPending = false;
                current.OnEnterRender();
                return;
            }

            current.Render();
        }
    }
}
