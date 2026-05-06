using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using UnityEngine;
using UnityEngine.VFX;

namespace MonoFSM.ParticleSystemActions
{
    public class VisualEffectEmitAction : AbstractRenderBehaviour
    {
        [SerializeField] VisualEffect _visualEffect;
        public bool _isPlay; //TODO: varboolwrapper?


        public override void OnEnterRenderImplement()
        {
            OnRenderImplement();
        }

        public override void OnRenderImplement()
        {
            if (_isPlay)
                _visualEffect.Play();
            else
                _visualEffect.Stop();
        }
    }
}
