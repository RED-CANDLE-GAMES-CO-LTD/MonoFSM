public interface IAbstractEventReceiver
{
    bool isActiveAndEnabled { get; }
}


//FIXME: 什麼時候需要沒有型別的？
public interface IRCGArgEventReceiver : IAbstractEventReceiver //Data Receiver
{
    public void EventReceived<T>(T arg); //讓繩子實作這個？
}

//EffectHitData會用有型別的
public interface IRCGArgEventReceiver<in T> : IAbstractEventReceiver //不行耶QQ，要Receiver也把Generic定義掉才行
{
    public void EventReceived(T arg);
    
}