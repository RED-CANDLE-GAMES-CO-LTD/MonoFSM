using MonoFSM.Core;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    //重疊期間每幀觸發（enter 那幀不觸發），hitData 重用 enter 時的同一顆 instance
    public sealed class EffectStayNode : AbstractEventHandler { }
}
