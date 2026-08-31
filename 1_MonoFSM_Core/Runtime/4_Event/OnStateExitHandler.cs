namespace MonoFSM.Core
{
    /// <summary>
    /// 離開這個 State 時執行底下的 Action，用來收拾進場時開啟的東西（清狀態、關特效、還原被改動的外部 Var）。
    /// 掛在 State 節點底下，與 OnStateEnter 成對使用。
    /// 只有走正常轉場（StateMachine.ChangeState）才會觸發：物件被 DespawnAction 直接回池時不會呼叫，
    /// 那種情境的收拾要另外掛在 despawn 那一側。
    /// </summary>
    public class OnStateExitHandler : AbstractEventHandler {}
}
