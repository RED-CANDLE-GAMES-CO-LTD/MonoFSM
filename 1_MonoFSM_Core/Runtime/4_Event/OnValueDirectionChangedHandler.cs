using UnityEngine;

namespace MonoFSM.Core
{
    public enum ValueChangeDirection
    {
        Increase = 0,
        Decrease = 1
    }

    /// <summary>
    ///     VarFloat 的值「變大」或「變小」時觸發（方向由 _direction 決定），EventHandle 的 arg 是變化量絕對值。
    ///     和 OnValueChangedHandler 的差別：
    ///     - OnValueChangedHandler 由 Field 的 listener 觸發，任何變化都跑，拿不到舊值所以判不出方向
    ///     - 這顆由 VarFloat.OnValueSet(old, new) 觸發，比得出方向、arg 帶得出變化量
    ///     同一顆 VarFloat 下可以掛多顆（ex: 一顆 Increase 給特效、一顆 Decrease 給音效）。
    ///     網路：OnValueSet 在 SetValueExecution 的共用路徑上，client 端被 NetworkedVarSync 寫入時同樣會觸發，
    ///     所以視覺表現做成 render action 就好，不需要 render sync。
    /// </summary>
    public class OnValueDirectionChangedHandler : AbstractEventHandler
    {
        [Tooltip("要往哪個方向變化才觸發")] public ValueChangeDirection _direction =
            ValueChangeDirection.Increase;

        public override string Description =>
            _direction == ValueChangeDirection.Increase ? "OnValueIncreased" : "OnValueDecreased";
    }
}
