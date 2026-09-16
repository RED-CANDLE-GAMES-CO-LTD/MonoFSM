using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    /// <summary>
    ///     Best Match 評分器：只讓「entity 上某顆 VarBool 等於指定值」的 receiver 進入排序，其餘一律排除
    ///     （回 float.MinValue，FindBestMatch 會跳過）。掛在 dealer 同一顆 GameObject 上，例如飛行怪偷竊用的
    ///     [Dealer] Gravity Grab 只想鎖定 d_IsGrabbed == true（玩家手上）的物件。
    ///     排序本身沿用 DefaultBestMatchScorer（priority + 距離）。
    ///     為什麼不用 EffectHitConditionWrapper：那條只在 enter 那一刻判一次、stay 不重判，
    ///     物件先進範圍再被玩家抓起會漏掉；scorer 每次 OnBestMatchCheck 都重算，才接得住狀態翻轉。
    /// </summary>
    public class BoolVarFilteredBestMatchScorer : DefaultBestMatchScorer
    {
        [Header("篩選")]
        [Tooltip("要檢查的 receiver entity 上的 bool 變數（用 tag 從對方 VariableFolder 找）")]
        [SOConfig("VariableType")]
        [SerializeField]
        private VariableTag _boolVarTag;

        [Tooltip("只有該變數等於此值的 receiver 才會進入排序")]
        [SerializeField]
        private bool _expectedValue = true;

        [Tooltip("entity 上找不到這顆變數、或該變數被 disable / inactive 時是否仍保留在排序內")]
        [SerializeField]
        private bool _keepWhenVarMissing;

        public enum FilterResult
        {
            Pass,
            NoBoolVarTag,
            NoBindEntity,
            VarMissing,
            VarDisabled,
            ValueMismatch,
        }

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private GeneralEffectReceiver _lastReceiver;

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private FilterResult _lastResult;

        public override float CalculateScore(
            GeneralEffectDealer dealer,
            GeneralEffectReceiver receiver
        )
        {
            _lastReceiver = receiver;
            _lastResult = Evaluate(receiver);
            if (_lastResult != FilterResult.Pass)
                return float.MinValue;

            return base.CalculateScore(dealer, receiver);
        }

        private FilterResult Evaluate(GeneralEffectReceiver receiver)
        {
            if (_boolVarTag == null)
                return FilterResult.NoBoolVarTag;

            var entity = receiver.BindEntity;
            if (entity == null)
                return FilterResult.NoBindEntity;

            var boolVar = entity.GetVar<VarBool>(_boolVarTag);
            if (boolVar == null)
                return _keepWhenVarMissing ? FilterResult.Pass : FilterResult.VarMissing;

            //disable / inactive 的 var 會留著上次的殘值，不能採信
            if (!boolVar.enabled || !boolVar.gameObject.activeInHierarchy)
                return _keepWhenVarMissing ? FilterResult.Pass : FilterResult.VarDisabled;

            return boolVar.Value == _expectedValue ? FilterResult.Pass : FilterResult.ValueMismatch;
        }
    }
}
