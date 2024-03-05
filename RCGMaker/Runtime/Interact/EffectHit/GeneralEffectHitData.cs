namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class GeneralEffectHitData : IEffectHitData
    {
        public IEffectDealer Dealer => _dealer;
        public IEffectReceiver Receiver => _receiver;

        private GeneralEffectDealer _dealer;
        private GeneralEffectReceiver _receiver;

        public void Override(IEffectDealer dealer, IEffectReceiver receiver)
        {
            _dealer = dealer as GeneralEffectDealer;
            _receiver = receiver as GeneralEffectReceiver;
        }
    }
}