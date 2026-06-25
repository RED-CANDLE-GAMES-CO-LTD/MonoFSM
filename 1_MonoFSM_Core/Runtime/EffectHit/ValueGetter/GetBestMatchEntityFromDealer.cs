using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime.EffectHit.ValueGetter
{
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
