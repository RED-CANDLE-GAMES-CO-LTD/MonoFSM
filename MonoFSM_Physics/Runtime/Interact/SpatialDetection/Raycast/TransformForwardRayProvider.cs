using System;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using UnityEngine;

namespace MonoFSM.Physics
{
    [Serializable]
    public class TransformForwardRayProvider : MonoBehaviour, IRayProvider
    {
        [SerializeField] private Transform _transform;

        public Ray GetRay()
        {
            // Create ray from camera through screen center
            if (_transform == null) _transform = transform; // Use the current GameObject's transform if not set
            var ray = new Ray(_transform.position, _transform.forward);
            return ray;
        }
    }
}