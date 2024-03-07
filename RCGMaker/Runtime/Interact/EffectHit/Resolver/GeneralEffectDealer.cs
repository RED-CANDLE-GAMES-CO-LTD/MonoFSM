using RCGMaker.Runtime.FSM._2_Variable;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    
    public class GeneralEffectDealer : EffectResolver, IEffectDealer
    {
        public FloatValueSource ValueSource;

        
        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            return ((GeneralEffectReceiver)receiver).EffectType == EffectType;
        }

        public float FinalValue => ValueSource.FinalValue;


        public void OnHitEnter(IEffectHitData data)
        {
            _enterNode?.OnEffectReceived(data);
        }

        public void OnHitExit(IEffectHitData data)
        {
            _exitNode?.OnEffectReceived(data);
        }
    }
}