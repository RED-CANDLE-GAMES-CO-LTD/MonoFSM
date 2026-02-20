namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    public class IsEffectResolverValidCondition : AbstractConditionBehaviour
    {
        public override string Description => _effectResolver != null
            ? $"{_effectResolver.name} IsValid"
            : "No Effect Resolver";

        [DropDownRef] public EffectResolver _effectResolver;
        protected override bool IsValid => _effectResolver.IsValid;
    }
}
