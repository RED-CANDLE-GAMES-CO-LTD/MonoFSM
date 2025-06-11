using UnityEngine;

namespace MonoFSM.Core.Simulate
{
    //FIXME: 如果有兩個 simulator會出問題耶
    public interface IUpdateSimulate //parent必須要有AbstractSimulator //好難喔..levelrunner, player, poolobject的要怎麼做？
    {
        void Simulate(float deltaTime);

        void AfterUpdate();

        bool isActiveAndEnabled { get; }

        GameObject gameObject { get; } 
    }
}