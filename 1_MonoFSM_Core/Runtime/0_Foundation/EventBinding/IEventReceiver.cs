// public interface IAbstractEventReceiver
// {
//     
// }

public interface IEventReceiver // : IAbstractEventReceiver //Data Receiver
{
    // public void EventReceived<T>(T arg); //讓繩子實作這個？
    void EventReceived();
    bool IsValid { get; }
    bool isActiveAndEnabled { get; }
}

public interface IArgEventReceiver<in T> : IEventReceiver //不行耶QQ，要Receiver也把Generic定義掉才行
{
    void ArgEventReceived(T arg);
}