namespace MonoFSM.Core.Runtime.Action
{
    /// <summary>
    /// 離開 state 時要收尾的 action 實作這個（例：move action 把自己寫的 FaceDir 清回 0）。
    /// GeneralState.OnExitState 會對子樹裡所有實作者呼叫（含關著的節點），走的是 simulate 的 ChangeState 路徑，
    /// 跟 OnActionExecuteImplement 同一條，所以網路權威 gate 一樣。
    /// 子樹可能有巢狀 state，實作者要自己檢查 exitingState == bindingState 再做事。
    /// </summary>
    public interface IActionStateExit
    {
        void OnBindingStateExit(GeneralState exitingState);
    }
}
