using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class SetParticleOneShotRender : AbstractRenderBehaviour, ISceneAwake
    {
        [Required]
        public ParticleSystem _particleSystem;
        public override void OnEnterRenderImplement()
        {
            //就 set active? 還是放在particle上就好不要做成renderBehaviour?
            _particleSystem.gameObject.SetActive(true);
            _particleSystem.Play(); //playOnAwake 關了，改明確觸發；預設 withChildren:true 會連動播 children
        }

        public override void OnRenderImplement()
        {
        }

        public void EnterSceneAwake()
        {
            //一次性播放：不循環、不靠 playOnAwake 自動播，播完用內建 stopAction 自動關掉自己的 GameObject（跟 OnEnterRenderImplement 開的是同一個），不用額外掛 callback
            //注意：stopAction 是root自己播完才觸發，若children時間比root長會被提前切斷，要確認root時間覆蓋children
            var main = _particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Disable;
        }
    }
}
