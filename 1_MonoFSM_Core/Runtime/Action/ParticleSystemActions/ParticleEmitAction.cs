using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.ParticleSystemActions
{
    public class ParticleEmitAction : AbstractRenderBehaviour
    {
        public override string Description =>
            $"Emit {_emitCount} particles from [{(_particleSystem != null ? _particleSystem.name : "null")}]";

        [SerializeField] [DropDownRef] [Required]
        private ParticleSystem _particleSystem;

        [SerializeField] [Min(1)] private int _emitCount = 10;

        // protected override void OnActionExecuteImplement()
        // {
        //     _particleSystem.Emit(_emitCount);
        // }

        public override void OnEnterRenderImplement()
        {
            _particleSystem.Emit(_emitCount);
        }

        public override void OnRenderImplement()
        {
            // throw new System.NotImplementedException();
        }
        
    }
}
