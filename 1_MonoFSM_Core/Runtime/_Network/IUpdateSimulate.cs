namespace MonoFSM_Core.Network
{
    //FIXME: 如果有兩個 simulator會出問題耶
    public interface IUpdateSimulate //parent必須要有AbstractSimulator //好難喔..levelrunner, player, poolobject的要怎麼做？
    {
        void Simulate(float deltaTime);

        void AfterUpdate();

        bool isActiveAndEnabled { get; } //fixme: 這個要不要放在MonoBehaviour裡面？還是放在AbstractSimulator裡面？ 這樣就可以知道有沒有被disable了
        //last simulate time?
    }
}