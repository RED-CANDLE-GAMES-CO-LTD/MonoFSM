using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime.EffectHit.ValueGetter
{
    /// <summary>
    ///     這個 Dealer 目前偵測到的「最佳目標」的 Entity（沒有就是 null）。
    ///     Getter 型、每次現算不留狀態，所以拿來當 VarEntity 的 value source，
    ///     再配 VarValueExistCondition 就是「範圍內有沒有目標」的 bool。
    ///     best match 由 EffectDetector 每次判定後呼叫 dealer.OnBestMatchCheck() 更新；
    ///     沒掛 AbstractOnlyTriggerBestMatch scorer 時預設用距離最近。
    /// </summary>
    public class GetBestMatchEntityFromDealer : AbstractValueSource<MonoEntity>
    {
        public override string Description => "GetBestMatch:" + _effectDealer.EffectType.name;
        public override MonoEntity Value => _effectDealer?.BestMatchReceiver?.BindEntity;

        [Required]
        [DropDownRef]
        public GeneralEffectDealer _effectDealer;

        //要檢查是不是有BestMatch
        protected override bool HasError()
        {
            return base.HasError()
                && (_effectDealer == null || _effectDealer?.IsOnlyTriggerBestMatch == false);
        }
    }
}
