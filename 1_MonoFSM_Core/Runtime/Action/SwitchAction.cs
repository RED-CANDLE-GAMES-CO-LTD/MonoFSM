using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    public enum SwitchMode
    {
        FirstMatch,
        AllMatch
    }

    public class SwitchAction : AbstractStateAction, IArgEventReceiver<GeneralEffectHitData>
    {
        public override string Description => $"Switch ({_mode})";

        [SerializeField]
        private SwitchMode _mode = SwitchMode.FirstMatch;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private SwitchCase[] _cases;

        protected override void OnActionExecuteImplement()
        {
            ExecuteSwitch(null);
        }

        void IArgEventReceiver<GeneralEffectHitData>.ArgEventReceived(GeneralEffectHitData arg)
        {
            ExecuteSwitch(arg);
        }

        private void ExecuteSwitch(GeneralEffectHitData arg)
        {
            AddEventRecord();
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

                if (arg != null)
                    switchCase.ExecuteActions(arg);
                else
                    switchCase.ExecuteActions();
                anyMatched = true;

                if (_mode == SwitchMode.FirstMatch)
                    return;
            }
        }
    }
}
