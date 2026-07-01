using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    /// <summary>
    /// 設定 ParticleSystem 的 Emission Rate Over Time。
    /// </summary>
    public class SetParticleEmissionRateAction : AbstractRenderBehaviour
    {
        public override string ValueInfo => "rate:" + _rateOverTime.Value * _multiplier;
        public override bool IsDrawingValueInfo => true;

        public override string Description =>
            $"Set emission rate over time to {_rateOverTime.Description} x{_multiplier} on [{(_particleSystem != null ? _particleSystem.name : "null")}]";

        [SerializeField] [DropDownRef] [Required]
        private ParticleSystem _particleSystem;

        [SerializeField] private VarFloatWrapper _rateOverTime;
        public float _multiplier = 1;

        public override void OnEnterRenderImplement()
        {
            if (_particleSystem == null)
            {
                Debug.LogWarning("SetParticleEmissionRateAction: ParticleSystem is null", this);
                return;
            }

            var emission = _particleSystem.emission;
            emission.rateOverTime = _rateOverTime.Value * _multiplier;
        }

        public override void OnRenderImplement()
        {
            OnEnterRenderImplement();
        }
    }
}
