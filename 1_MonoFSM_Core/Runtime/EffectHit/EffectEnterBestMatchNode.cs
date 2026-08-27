using MonoFSM.Runtime.Interact.EffectHit;

namespace _1_MonoFSM_Core.Runtime.EffectHit
{
    /// <summary>
    /// Fires when this resolver becomes the dealer's best match and exposes the matched entity to child actions.
    /// </summary>
    public class EffectEnterBestMatchNode : AbstractEffectEnterNode
    {
        protected override string HitEntityLabel => "bestMatch hitEntity";
    }
}
