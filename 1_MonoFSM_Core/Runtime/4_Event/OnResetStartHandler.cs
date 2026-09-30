using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Core
{
    /// <summary>
    ///     IResetStart 事件：root MonoObj 的 ResetStart 時觸發子 action。時機是 WorldInit 尾段的 WorldReset
    ///     （開場一次，每個 peer 各自跑）和每次 level reset，都在 ResetLevelRestore（Var 回預設值）之後。
    ///     不吃 simulation culling gate（IgnoreCulling）：開場玩家還沒生成、遠方物件一定是 cull 狀態，
    ///     初始化要照樣跑。但 action 本身若掛在 Simulation Culling Active Handle 底下（被 SetActive(false)）還是不會跑，
    ///     初始化用的 action 要放在 handle 外面。其他 gate 照舊：沒 state authority 的 client 只跑 render、不跑 action。
    /// </summary>
    public class OnResetStartHandler : AbstractEventHandler, IResetStart
    {
        protected override bool IgnoreCulling => true;

        public void ResetStart()
        {
            EventHandle();
        }
    }
}
