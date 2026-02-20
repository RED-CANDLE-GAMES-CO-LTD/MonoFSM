using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action
{
    public class SwitchCase : AbstractDescriptionBehaviour
    {
        protected override string DescriptionTag => "Case";

        public override string Description => _isDefault ? "Default" : base.Description;

        [SerializeField] private bool _isDefault;

        [HideIf(nameof(_isDefault))] [AutoChildren(DepthOneOnly = true)] [CompRef]
        private AbstractConditionBehaviour[] _conditions;

        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private IEventReceiver[] _actions;

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
    }
}
