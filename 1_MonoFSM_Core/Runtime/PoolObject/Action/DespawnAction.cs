using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.ObjectPool
{
    /// <summary>
    ///     把自己回收掉
    /// FIXME: Scene Obj Reset FSM有辦法回來嗎？p
    /// </summary>
    public class DespawnAction : AbstractArgEventHandler<GeneralEffectHitData>
    {
        // [ShowIf(nameof(_effectResolver))] //FIXME: 還是用Disable?
        public EffectHitTarget _despawnTarget = EffectHitTarget.Receiver;

        protected override void OnActionExecuteImplement()
        {
            Debug.Log("DespawnAction", this);
            _parentObj.Despawn();
            // WorldUpdateSimulator.CurrentTick
        }

        protected override void OnArgEventReceived(GeneralEffectHitData arg)
        {
            switch (_despawnTarget)
            {
                //去把對方物件給回收掉，很兇耶
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
