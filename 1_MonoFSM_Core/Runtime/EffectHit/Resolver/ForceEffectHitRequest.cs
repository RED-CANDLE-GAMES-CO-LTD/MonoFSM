using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    /// <summary>
    /// 「在 tick 內對 receiver 打一發 ForceDirectEffectHit」的請求，排進 TickActionQueue 用。
    /// 走的是跟 PlayerInteractState / ForceTriggerEffectAction 一樣的路（CanHitReceiver → IsValid → EnterNode），
    /// 只是從 tick 外（Editor probe `up hit`、scenario runner）發起。執行結果寫回自己的欄位，呼叫端輪詢。
    /// receiver 所在的 MonoObj 還沒在模擬（simulation culling / ShouldSimulte=false / 不在 simulator）時，
    /// EnterNode 底下的 action 會被 AbstractEventHandler 靜靜跳過、但 HitEnter 照記 —— 所以先延後，最多
    /// _maxDeferTicks 個 tick，逾時算失敗。
    /// </summary>
    public class ForceEffectHitRequest : ITickAction
    {
        public const string ReasonInactive = "receiver GameObject inactive";
        public const string ReasonNotInSimulator = "receiver 的 MonoObj 不在 simulator 裡（IsActiveInSimulator=false）";
        public const string ReasonSimulationCulling = "receiver 的 MonoObj 在 simulation culling 中（IsSimulationCulling，CullingEventTarget 還沒判定 near）";
        public const string ReasonNoAuthority = "receiver 的 MonoObj ShouldSimulte=false（沒有 authority / 沒人 push）";

        public readonly int _id;
        public readonly GeneralEffectReceiver _receiver;
        public readonly GeneralEffectDealer _dealer;
        public readonly int _maxDeferTicks;
        private readonly MonoObj _receiverObj;

        public bool _executed;
        public int _executedTick = -1;
        public bool _canHit;
        public bool _timedOut;
        public string _failReason;

        /// <summary>被延後了幾個 tick，跟最後一次延後的原因（常數字串）。</summary>
        public int _deferredTicks;
        public string _deferReason;

        public ForceEffectHitRequest(int id, GeneralEffectReceiver receiver, GeneralEffectDealer dealer,
            int maxDeferTicks = 120)
        {
            _id = id;
            _receiver = receiver;
            _dealer = dealer;
            _maxDeferTicks = maxDeferTicks;
            _receiverObj = receiver != null ? receiver.GetComponentInParent<MonoObj>(true) : null;
        }

        //跟 AbstractEventHandler.EventHandleImplement 的 gate 同一套（culling / ShouldSimulate），
        //只是那邊是靜靜跳過，這裡先擋下來等
        private string NotReadyReason()
        {
            if (!_receiver.gameObject.activeInHierarchy)
                return ReasonInactive;
            if (_receiverObj == null)
                return null;
            if (!_receiverObj.IsActiveInSimulator)
                return ReasonNotInSimulator;
            if (_receiverObj.IsSimulationCulling)
                return ReasonSimulationCulling;
            if (!_receiverObj.ShouldSimulte)
                return ReasonNoAuthority;
            return null;
        }

        public bool ExecuteInTick(int tick)
        {
            if (_receiver == null || _dealer == null)
            {
                Finish(tick, false, _receiver == null ? "receiver 已被 Destroy" : "dealer 已被 Destroy");
                return true;
            }

            var notReady = NotReadyReason();
            if (notReady != null)
            {
                _deferReason = notReady;
                if (_deferredTicks >= _maxDeferTicks)
                {
                    _timedOut = true;
                    Finish(tick, false, notReady);
                    return true;
                }

                _deferredTicks++;
                return false;
            }

            Finish(tick, _receiver.ForceDirectEffectHit(_dealer, null), null);
            if (_failReason == null)
                _failReason = _dealer.FailReason;
            return true;
        }

        private void Finish(int tick, bool canHit, string reason)
        {
            _executed = true;
            _executedTick = tick;
            _canHit = canHit;
            _failReason = reason;
        }
    }
}
