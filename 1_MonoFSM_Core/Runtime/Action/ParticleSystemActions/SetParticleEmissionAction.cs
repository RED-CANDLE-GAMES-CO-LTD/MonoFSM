using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class SetParticleEmissionAction : AbstractStateAction
    {
        public override string Description =>
            $"Set emission {(_enabled.Description)} on [{(_particleSystem != null ? _particleSystem.name : "null")}]";

        [SerializeField] [DropDownRef] [Required]
        private ParticleSystem _particleSystem;

        [SerializeField] private VarBoolWrapper _enabled;

        protected override void OnActionExecuteImplement()
        {
            if (_particleSystem == null)
            {
                Debug.LogWarning("SetParticleEmissionAction: ParticleSystem is null", this);
                return;
            }

            var emission = _particleSystem.emission;
            emission.enabled = _enabled.Value;
        }
    }
}
