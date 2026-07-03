using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// VarFloatCountDownTimer 倒數到 Min（時間到）就是 true。
    /// 比起綁 VarFloat + VarFloatCompareConstCondition，直接引用 timer 更單純。
    /// </summary>
    public class IsTimerUpCondition : AbstractConditionBehaviour
    {
        public override string Description =>
            $"Timer Up [{(_timer != null ? _timer.name : "?")}]";

        [Required]
        [DropDownRef]
        [SerializeField] private VarFloatCountDownTimer _timer;

        [ShowInInspector] private bool IsTimerUp => IsValid;

        protected override bool IsValid => isActiveAndEnabled && _timer != null && _timer.IsTimerUp;
    }
}
