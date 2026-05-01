using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    public class SwitchCase : AbstractDescriptionBehaviour, IActionParent
    {
        protected override string DescriptionTag => "Case";

        public override string Description => _isDefault ? "Default" : base.Description;

        [SerializeField] private bool _isDefault;

        [HideIf(nameof(_isDefault))] [AutoChildren(DepthOneOnly = true)] [CompRef]
        private AbstractConditionBehaviour[] _conditions;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private IEventReceiver[] _actions;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private IRenderBehaiour[] _renderBehaiours;

        public bool IsDefault => _isDefault;

        public bool IsConditionMet => !_isDefault && _conditions.IsAllValid();

        public void ExecuteActions()
        {
            foreach (var action in _actions)
            {
                if (action == null) continue;
                if (action.IsValid)
                    action.EventReceived();
            }
        }

        public void Render()
        {
            foreach (var renderBehaiour in _renderBehaiours)
            {
                if (renderBehaiour == null) continue;
                renderBehaiour.OnRender();
            }
        }

        public void ExecuteActions(GeneralEffectHitData arg)
        {
            foreach (var action in _actions)
            {
                if (action == null) continue;
                if (!action.IsValid) continue;
                if (action is IArgEventReceiver<GeneralEffectHitData> argReceiver)
                    argReceiver.ArgEventReceived(arg);
                else
                    action.EventReceived();
            }
        }
    }
}
