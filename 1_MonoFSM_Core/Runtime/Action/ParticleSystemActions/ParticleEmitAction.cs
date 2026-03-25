using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class ParticleEmitAction : AbstractStateAction
    {
        public override string Description =>
            $"Emit {_emitCount} particles from [{(_particleSystem != null ? _particleSystem.name : "null")}]";

        [SerializeField] [DropDownRef] [Required]
        private ParticleSystem _particleSystem;

        [SerializeField] [Min(1)] private int _emitCount = 10;

        protected override void OnActionExecuteImplement()
        {
            if (_particleSystem == null)
            {
                Debug.LogWarning("ParticleEmitAction: ParticleSystem is null", this);
                return;
            }

            _particleSystem.Emit(_emitCount);
        }
    }
}
