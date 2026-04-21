using MonoFSM.Core.Runtime.Action;
using UnityEngine;
using UnityEngine.VFX;

namespace MonoFSM.ParticleSystemActions
{
    public class VisualEffectEmitAction : AbstractStateAction
    {
        [SerializeField] VisualEffect _visualEffect;
        public bool _isPlay;

        protected override void OnActionExecuteImplement()
        {
            if (_isPlay)
                _visualEffect.Play();
            else
                _visualEffect.Stop();
        }
    }
}
