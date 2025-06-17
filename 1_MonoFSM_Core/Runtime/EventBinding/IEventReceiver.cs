public interface IAbstractEventReceiver
{
    bool isActiveAndEnabled { get; }
}

//FIXME:改名ㄅ IEffectReceiver?
//FIXME: 什麼時候需要沒有型別的？
public interface IEventReceiver : IAbstractEventReceiver //Data Receiver
{
    public void EventReceived<T>(T arg); //讓繩子實作這個？
    public void EventReceived();
    bool IsValid { get; }
}

//IArgEventListener?
//EffectHitData會用有型別的
//FIXME: 感覺沒有整好？根本不需要這個？用上面那種就夠了嗎？但這樣我型別才能事先定義好像還是比較好耶
public interface IArgEventReceiver<in T> : IEventReceiver //不行耶QQ，要Receiver也把Generic定義掉才行
{
    public void ArgEventReceived(T arg);
}