using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.ObjectPool
{
    //FIXME: Despawn action?
    /// <summary>
    ///     把自己回收掉
    /// </summary>
    public class DespawnAction : AbstractArgEventHandler<GeneralEffectHitData>
    {
        [HideIf(nameof(_effectResolver))] [Required] [PreviewInInspector] [AutoParent]
        private MonoObj _object;

        public EffectHitTarget _despawnTarget = EffectHitTarget.Receiver;

        protected override void OnActionExecuteImplement()
        {
            // Debug.Log("ReturnToPoolAction", this);
            _object.Despawn();
            // WorldUpdateSimulator.CurrentTick
        }

        protected override void OnArgEventReceived(GeneralEffectHitData arg)
        {
            switch (_despawnTarget)
            {
                case EffectHitTarget.Dealer:
                    arg.GeneralDealer.DespawnParentObj();
                    break;
                case EffectHitTarget.Receiver:
                    arg.GeneralReceiver.DespawnParentObj();
                    break;
            }
        }
    }
}
