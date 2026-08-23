using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Resolver
{
    /// <summary>
    /// 把一般的 AbstractConditionBehaviour 包成 pair 條件（EffectHitCondition）。
    /// 差別在判斷時機：resolver 上的 _conditions 每幀被 EffectDetector 監看，變 false 會對「已經 enter 的
    /// receiver」補發 exit；掛在這裡的條件只在 CanHitReceiver（enter 那一刻）判一次，之後走 stay 不重判。
    /// 需要「觸發後就不再吃新目標、但不能踢掉剛觸發的那個」時用這支（ex: 台座只能掛一個東西）。
    /// </summary>
    public class EffectHitConditionWrapper : AbstractEffectHitCondition
    {
        [CompRef]
        [PreviewInInspector]
        [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionBehaviour[] _conditions =
            System.Array.Empty<AbstractConditionBehaviour>();

        protected override bool IsEffectHitValidImplement(EffectResolver receiver)
        {
            if (_conditions == null || _conditions.Length == 0)
            {
                Debug.LogWarning(
                    "[EffectHitConditionWrapper] 沒有子 Condition，一律通過，可能是忘了掛",
                    this
                );
                return true;
            }

            return _conditions.IsAllValid(this);
        }
    }
}
