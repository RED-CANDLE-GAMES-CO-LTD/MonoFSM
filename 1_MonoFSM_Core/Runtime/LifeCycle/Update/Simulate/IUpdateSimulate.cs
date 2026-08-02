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
        //FIXME: proxy 不該跑？
        void Simulate(float deltaTime);

        // void AfterUpdate();

        bool isActiveAndEnabled { get; }
        bool IsValid => isActiveAndEnabled;
        bool IsUpdating => isActiveAndEnabled;
        string name { get; }
        GameObject gameObject { get; }

        /// <summary>
        /// 排序順序，數字越小越先執行。預設為0。
        /// </summary>
        int SimulateOrder => 0;
    }

    /// <summary>
    /// Culling 語意上等同「從模擬中消失」，跟 disable 同級：被 cull 的那一刻整棵子樹就不再 tick，
    /// 但 GameObject 不見得是 inactive（cullingHandle 可能是兄弟節點，或 cull 是從 parent 傳下來的），
    /// 所以 OnDisable 收不到。需要在消失時收尾的組件（ex: EffectDetector 補送 exit）實作這個介面。
    /// 由 MonoObj 對自己 scope 的子樹廣播，一次 cull 只會呼叫一次。
    /// </summary>
    public interface ICullingEnterHandler
    {
        void OnCullingEnter();
    }

    public interface IRenderUpdate //不對吧？
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
