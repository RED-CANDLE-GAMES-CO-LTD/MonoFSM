using MonoFSM.Core.Simulate;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    public class SwitchCaseActionSimulator : AbstractEventHandler, IUpdateSimulate, IActionParent
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

            if (!anyMatched && defaultCase != null)
                defaultCase.ExecuteActions();
        }
    }
}
