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

        [Tooltip("將起點往後退 radius 距離，讓 SphereCast 能偵測到起點重疊的 collider")]
        public bool _checkOriginOverlap;

        [ShowInInspector]
        public float GetRadius()
        {
            return _sphereCollider != null ? _sphereCollider.radius : _radius;
        }

        private ISphereCastProcessor sphereCastProcessor =>
            _parentObj.WorldUpdateSimulator.GetCompCache<ISphereCastProcessor>();

        protected override int PerformCast(Ray ray, float distance, RaycastHit[] results)
        {
            var origin = ray.origin;
            var castDistance = distance;
            var radius = GetRadius();

            if (_checkOriginOverlap)
            {
                // 把起點往後退 radius，讓原本重疊的 collider 進入 SphereCast 的掃掠範圍
                origin -= ray.direction * radius;
                castDistance += radius;
            }

            return sphereCastProcessor.SphereCastNonAlloc(
                origin,
                radius,
                ray.direction,
                results,
                castDistance,
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
