using MonoFSM.Core.Attributes;
using MonoFSM.PhysicsWrapper;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    /// <summary>
    ///     BoxCast 偵測器，繼承 AbstractCastCache。
    /// </summary>
    public class BoxCastCache : AbstractCastCache
    {
        [Auto]
        [SerializeField]
        private BoxCollider _boxCollider;

        [HideIf("@_boxCollider != null")]
        [SerializeField]
        private Vector3 _halfExtents = Vector3.one * 0.25f;

        [ShowInInspector]
        public Vector3 GetHalfExtents()
        {
            return _boxCollider != null ? _boxCollider.size * 0.5f : _halfExtents;
        }

        private readonly RaycastHit[] _boxCastResults = new RaycastHit[10];

        private IBoxCastProcessor boxCastProcessor =>
            _parentObj.WorldUpdateSimulator.GetCompCache<IBoxCastProcessor>();

        protected override bool PerformCast(Ray ray, float distance, out RaycastHit hitInfo)
        {
            var hitCount = boxCastProcessor.BoxCastNonAlloc(
                ray.origin,
                GetHalfExtents(),
                ray.direction,
                _boxCastResults,
                transform.rotation,
                distance,
                _hittingLayer,
                _queryTriggerInteraction
            );

            if (hitCount > 0)
            {
                hitInfo = _boxCastResults[0];
                return true;
            }

            hitInfo = default;
            return false;
        }

        protected override void DrawCastGizmo(Ray ray, float distance)
        {
            var halfExtents = GetHalfExtents();
            var rotation = transform.rotation;

            Gizmos.matrix = Matrix4x4.TRS(ray.origin, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;

            var endPoint = ray.origin + ray.direction * distance;
            Gizmos.matrix = Matrix4x4.TRS(endPoint, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }

        protected override string DescriptionTag => "BoxCast";
    }
}
