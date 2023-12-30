public interface IAbstractEventReceiver
{
}

public interface IRCGArgEventReceiver : IAbstractEventReceiver //Data Receiver
{
    public void EventReceived<T>(T arg); //讓繩子實作這個？
}

public interface IRCGArgEventReceiver<in T> : IAbstractEventReceiver //不行耶QQ，要Receiver也把Generic定義掉才行
{
    public void EventReceived(T arg);
}