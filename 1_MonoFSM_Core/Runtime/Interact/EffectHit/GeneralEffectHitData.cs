using System;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using RCGMaker.Runtime.Item_BuildSystem;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public interface IActor
    {
    }

    [Serializable]
    public class GeneralEffectHitData : IEffectHitData
    {
        public static GeneralEffectHitData Borrow(IEffectDealer dealer, IEffectReceiver receiver)
        {
            var data = new GeneralEffectHitData();
            data.Override(dealer, receiver);
            return data;
        }

        public IEffectDealer Dealer => _dealer;
        public IEffectReceiver Receiver => _receiver;

        public IActor Source => _dealer.Owner;
        public IActor Target => _receiver.Owner;

        public GeneralEffectDealer GeneralDealer => _dealer;
        public GeneralEffectReceiver GeneralReceiver => _receiver;

        private GeneralEffectDealer _dealer;
        private GeneralEffectReceiver _receiver;

        public void Override(IEffectDealer dealer, IEffectReceiver receiver)
        {
            _dealer = dealer as GeneralEffectDealer;
            _receiver = receiver as GeneralEffectReceiver;
        }

        public T GetComponentFromDealerOwner<T>() where T : class
        {
            return GeneralDealer.GetComponentOfSibling<IModuleOwner, T>();
        }

        public T GetComponentFromReceiver<T>() where T : class
        {
            return GeneralReceiver.GetComponentOfSibling<IModuleOwner, T>();
        }
    }
}