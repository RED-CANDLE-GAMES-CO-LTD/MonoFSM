namespace MonoFSM.Core.EffectHit
{
    public interface IEffectDealerProvider
    {
        public IEffectDealer[] Dealers { get; }

        //沒有實作...extension method?
    }

    public interface IEffectReceiverProvider
    {
        IEffectReceiver[] Receivers { get; }
    }
}