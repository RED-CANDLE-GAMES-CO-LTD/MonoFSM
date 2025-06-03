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
}

//IArgEventListener?
//EffectHitData會用有型別的
public interface IArgEventReceiver<in T> : IEventReceiver //不行耶QQ，要Receiver也把Generic定義掉才行
{
    public void ArgEventReceived(T arg);
}