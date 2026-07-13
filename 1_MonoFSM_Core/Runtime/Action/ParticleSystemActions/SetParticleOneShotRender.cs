using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class SetParticleOneShotRender : AbstractRenderBehaviour, ISceneAwake
    {
        public ParticleSystem _particleSystem;
        public override void OnEnterRenderImplement()
        {
            //就 set active? 還是放在particle上就好不要做成renderBehaviour?
            _particleSystem.gameObject.SetActive(true);
        }

        public override void OnRenderImplement()
        {
        }

        public void EnterSceneAwake()
        {
            //FIXME; 把root particle改 disable callback?
            //loop false

            // _particleSystem.main.loop = false;
        }
    }
}
