using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    //這個 receiver 目前是否被任一 dealer 選為 best match（拉式，給 VarBool 的 valueSource 用）
    public class IsBestMatchedReceiverCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        [SerializeField]
        private GeneralEffectReceiver _receiver;

        protected override bool IsValid => _receiver != null && _receiver.IsBestMatched;

        public override string Description =>
            $"Is BestMatched [{(_receiver != null ? _receiver.name : "?")}]";
    }
}
