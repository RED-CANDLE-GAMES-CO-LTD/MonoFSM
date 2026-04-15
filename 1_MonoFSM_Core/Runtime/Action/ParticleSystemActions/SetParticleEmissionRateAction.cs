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
    public class SetParticleEmissionRateAction : AbstractStateAction
    {
        public override string Description =>
            $"Set emission rate over time to {_rateOverTime.Description} x{_multiplier} on [{(_particleSystem != null ? _particleSystem.name : "null")}]";

        [SerializeField] [DropDownRef] [Required]
        private ParticleSystem _particleSystem;

        [SerializeField] private VarFloatWrapper _rateOverTime;
        public float _multiplier = 1;

        protected override void OnActionExecuteImplement()
        {
            if (_particleSystem == null)
            {
                Debug.LogWarning("SetParticleEmissionRateAction: ParticleSystem is null", this);
                return;
            }

            var emission = _particleSystem.emission;
            emission.rateOverTime = _rateOverTime.Value * _multiplier;
        }
    }
}
