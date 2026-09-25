using MonoFSM.Foundation;

namespace Fusion.Addons.KCC.ECM2.Examples.Networking.Fusion_v2.Characters.Scripts.Input
{
    /// <summary>
    /// 把一顆 VarInt 轉成 float 給 VarFloat 當 value source（掛在 [Getter] VarFloat 底下）。
    /// 用在「數值本身是整數 Var，但下游 Action / Condition 只吃 VarFloat」的時候，例如整數價格拿去跟 d_Money 比較、扣款。
    /// </summary>
    public class IntToFloatValueSource : AbstractValueSource<float>
    {
        public VarInt _intVar;
        public override float Value => _intVar != null ? _intVar.Value : 0f;
    }
}
