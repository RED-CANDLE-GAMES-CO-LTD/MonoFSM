using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.PhysicsWrapper
{
    public class LockOnTargetRayProvider : AbstractRayProvider
    {
        [SerializeField]
        private VarTransform _lockOnTarget;

        public Transform _muzzle;
        // private Transform _characterTransform;

        [ShowInPlayMode]
        private Transform Target => _lockOnTarget?.Value;

        public override Ray GetRay()
        {

            if (Target == null)
                // Fallback to forward direction if no target
                return new Ray(_muzzle.position, _muzzle.forward);

            // Calculate direction from character to target
            var direction = (Target.position - _muzzle.position).normalized;

            return new Ray(_muzzle.position, direction);
        }
    }
}
