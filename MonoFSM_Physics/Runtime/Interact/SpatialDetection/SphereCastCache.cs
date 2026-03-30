using MonoFSM.Core.Attributes;
using MonoFSM.PhysicsWrapper;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    /// <summary>
    ///     SphereCast 偵測器，繼承 AbstractCastCache。
    /// </summary>
    public class SphereCastCache : AbstractCastCache
    {
        [Auto]
        [SerializeField]
        private SphereCollider _sphereCollider;

        [HideIf("@_sphereCollider != null")]
        [SerializeField]
        private float _radius = 0.5f;

        [ShowInInspector]
        public float GetRadius()
        {
            return _sphereCollider != null ? _sphereCollider.radius : _radius;
        }

        private ISphereCastProcessor sphereCastProcessor =>
            _parentObj.WorldUpdateSimulator.GetCompCache<ISphereCastProcessor>();

        protected override bool PerformCast(Ray ray, float distance, out RaycastHit hitInfo)
        {
            return sphereCastProcessor.SphereCast(
                ray.origin,
                GetRadius(),
                ray.direction,
                out hitInfo,
                distance,
                _hittingLayer,
                _queryTriggerInteraction
            );
        }

        protected override void DrawCastGizmo(Ray ray, float distance)
        {
            var radius = GetRadius();
            Gizmos.DrawWireSphere(ray.origin, radius);

            var endPoint = ray.origin + ray.direction * distance;
            Gizmos.DrawWireSphere(endPoint, radius);
        }

        protected override string DescriptionTag => "SphereCast";
    }
}
