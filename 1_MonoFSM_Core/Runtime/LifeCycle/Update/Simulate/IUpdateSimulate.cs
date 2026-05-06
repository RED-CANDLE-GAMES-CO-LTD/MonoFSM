using UnityEngine;

namespace MonoFSM.Core.Simulate
{
    public interface IBeforeSimulate //parent必須要有AbstractSimulator
    {
        void BeforeSimulate(float deltaTime);
        bool isActiveAndEnabled { get; }

        GameObject gameObject { get; }

        /// <summary>
        /// 排序順序，數字越小越先執行。預設為0。
        /// </summary>
        int BeforeSimulateOrder => 0;
    }

    public interface IAfterSimulate
    {
        void AfterSimulate(float deltaTime);
        bool isActiveAndEnabled { get; }

        GameObject gameObject { get; }
    }

    //FIXME: 如果有兩個 simulator會出問題耶
    //FIXME: 拆asmdef的話要怎麼做？ LifeCycle
    public interface IUpdateSimulate //parent必須要有AbstractSimulator //好難喔..levelrunner, player, poolobject的要怎麼做？
    {

        void Simulate(float deltaTime);

        // void AfterUpdate();

        bool isActiveAndEnabled { get; }
        bool IsValid => isActiveAndEnabled;
        string name { get; }
        GameObject gameObject { get; }

        /// <summary>
        /// 排序順序，數字越小越先執行。預設為0。
        /// </summary>
        int SimulateOrder => 0;
    }

    public interface IRenderSimulate : RenderInvoker
    {
        void Render(float runnerLocalRenderTime);
        bool isActiveAndEnabled { get; }
        GameObject gameObject { get; }
    }


    public interface IAfterRenderMono
    {
        void AfterRender();
        public bool isActiveAndEnabled { get; }
        GameObject gameObject { get; }
    }

    // public interface IAfterUpdate
    // {
    //
    // }
}
