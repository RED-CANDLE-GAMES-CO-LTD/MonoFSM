using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    /// FIXME: 不該叫simulator
    /// 和 AbstractConditionActivateRunner 整合？
    /// </summary>
    public class SwitchCaseActionSimulator : AbstractDescriptionBehaviour, IUpdateSimulate,
        IRenderSimulate
    {
        protected override string DescriptionTag => "Switch Simulate";

        public override string Description => $"Switch ({_mode})";

        [SerializeField] private SwitchMode _mode = SwitchMode.FirstMatch;

        [AutoChildren(DepthOneOnly = true)] [CompRef]
        private SwitchCase[] _cases;

        public void Simulate(float deltaTime)
        {
            SwitchCase defaultCase = null;
            bool anyMatched = false;

            foreach (var switchCase in _cases)
            {
                if (switchCase == null || !switchCase.gameObject.activeSelf)
                    continue;

                if (switchCase.IsDefault)
                {
                    defaultCase = switchCase;
                    continue;
                }

                if (!switchCase.IsConditionMet)
                    continue;

                switchCase.ExecuteActions();
                anyMatched = true;

                if (_mode == SwitchMode.FirstMatch)
                    return;
            }

            // if (!anyMatched && defaultCase != null)
            //     defaultCase.ExecuteActions();
        }

        //FIXME: 要每種特別做嗎？
        public void Render(float runnerLocalRenderTime)
        {
            SwitchCase defaultCase = null;
            bool anyMatched = false;

            foreach (var switchCase in _cases)
            {
                if (switchCase == null || !switchCase.gameObject.activeSelf)
                    continue;

                // if (switchCase.IsDefault)
                // {
                //     defaultCase = switchCase;
                //     continue;
                // }

                if (!switchCase.IsConditionMet)
                    continue;

                switchCase.Render();
                anyMatched = true;

                if (_mode == SwitchMode.FirstMatch)
                    return;
            }
            //
            // if (!anyMatched && defaultCase != null)
            //     defaultCase.Render();
        }
    }
}
