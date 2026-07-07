using MonoFSM.Core.Simulate;
using MonoFSM.Variable.Attributes;
using UnityEngine;
using UnityEngine.Internal;

namespace MonoFSM.PhysicsWrapper
{
    public interface IBoxCastProcessor
    {
        public int BoxCastNonAlloc(
            Vector3 origin,
            Vector3 halfExtents,
            Vector3 direction,
            RaycastHit[] results,
            Quaternion orientation,
            [DefaultValue("Mathf.Infinity")] float maxDistance,
            [DefaultValue("DefaultRaycastLayers")] int layerMask,
            [DefaultValue("QueryTriggerInteraction.UseGlobal")]
                QueryTriggerInteraction queryTriggerInteraction
        );
    }

    public interface ICapsuleRaycastProcessor
    {
        public int CapsuleCastNonAlloc(
            Vector3 point1,
            Vector3 point2,
            float radius,
            Vector3 direction,
            RaycastHit[] results,
            [DefaultValue("Mathf.Infinity")] float maxDistance,
            [DefaultValue("DefaultRaycastLayers")] int layerMask,
            [DefaultValue("QueryTriggerInteraction.UseGlobal")]
                QueryTriggerInteraction queryTriggerInteraction
        );
    }

    public interface ISphereOverlapProcessor
    {
        public int OverlapSphereNonAlloc(
            Vector3 position,
            float radius,
            Collider[] results,
            [DefaultValue("DefaultRaycastLayers")] int layerMask,
            [DefaultValue("QueryTriggerInteraction.UseGlobal")]
                QueryTriggerInteraction queryTriggerInteraction
        );
    }

    public interface IBoxOverlapProcessor
    {
        public int OverlapBoxNonAlloc(
            Vector3 center,
            Vector3 halfExtents,
            Collider[] results,
            [DefaultValue("Quaternion.identity")] Quaternion orientation,
            [DefaultValue("DefaultRaycastLayers")] int layerMask,
            [DefaultValue("QueryTriggerInteraction.UseGlobal")]
                QueryTriggerInteraction queryTriggerInteraction
        );
    }

    public interface IOverlapProcessor
        : ICapsuleOverlapProcessor,
            ISphereOverlapProcessor,
            IBoxOverlapProcessor { }

    public interface ICapsuleOverlapProcessor
    {
        public int OverlapCapsuleNonAlloc(
            Vector3 point1,
            Vector3 point2,
            float radius,
            Collider[] results,
            [DefaultValue("DefaultRaycastLayers")] int layerMask,
            [DefaultValue("QueryTriggerInteraction.UseGlobal")]
                QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
        );
    }

    public class MyOverlap : MonoBehaviour
    {
        private IOverlapProcessor _overlapProcessor;

        private IOverlapProcessor OverlapProcessor =>
            _overlapProcessor ??= WorldUpdateSimulator
                .GetWorldUpdateSimulator(gameObject)
                ?.GetCompCache<IOverlapProcessor>();

        /// <summary>
        /// Find all colliders touching or inside the capsule.
        /// </summary>
        /// <param name="point1">The center of the sphere at the start of the capsule.</param>
        /// <param name="point2">The center of the sphere at the end of the capsule.</param>
        /// <param name="radius">The radius of the spheres and capsule.</param>
        /// <param name="results">The buffer to store the results in.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a capsule.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        /// <returns>The number of colliders stored in the results buffer.</returns>
        public int OverlapCapsuleNonAlloc(
            Vector3 point1,
            Vector3 point2,
            float radius,
            Collider[] results,
            int layerMask = -1,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
        )
        {
            if (OverlapProcessor != null)
                return OverlapProcessor.OverlapCapsuleNonAlloc(
                    point1,
                    point2,
                    radius,
                    results,
                    layerMask,
                    queryTriggerInteraction
                );

            // Fallback to Physics.OverlapCapsuleNonAlloc if no processor is available

            return Physics.OverlapCapsuleNonAlloc(
                point1,
                point2,
                radius,
                results,
                layerMask,
                queryTriggerInteraction
            );
        }

        /// <summary>
        /// Find all colliders touching or inside the sphere.
        /// </summary>
        /// <param name="position">Center of the sphere.</param>
        /// <param name="radius">Radius of the sphere.</param>
        /// <param name="results">The buffer to store the results in.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a sphere.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        /// <returns>The number of colliders stored in the results buffer.</returns>
        public int OverlapSphereNonAlloc(
            Vector3 position,
            float radius,
            Collider[] results,
            int layerMask = -1,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
        )
        {
            if (OverlapProcessor != null)
                return OverlapProcessor.OverlapSphereNonAlloc(
                    position,
                    radius,
                    results,
                    layerMask,
                    queryTriggerInteraction
                );

            // Fallback to Physics.OverlapSphereNonAlloc if no processor is available
            return Physics.OverlapSphereNonAlloc(
                position,
                radius,
                results,
                layerMask,
                queryTriggerInteraction
            );
        }

        /// <summary>
        /// Find all colliders touching or inside the box.
        /// </summary>
        /// <param name="center">Center of the box.</param>
        /// <param name="halfExtents">Half the size of the box in each dimension.</param>
        /// <param name="results">The buffer to store the results in.</param>
        /// <param name="orientation">Rotation of the box.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a box.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        /// <returns>The number of colliders stored in the results buffer.</returns>
        public int OverlapBoxNonAlloc(
            Vector3 center,
            Vector3 halfExtents,
            Collider[] results,
            Quaternion orientation = default,
            int layerMask = -1,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
        )
        {
            if (OverlapProcessor != null)
                return OverlapProcessor.OverlapBoxNonAlloc(
                    center,
                    halfExtents,
                    results,
                    orientation,
                    layerMask,
                    queryTriggerInteraction
                );

            // Fallback to Physics.OverlapBoxNonAlloc if no processor is available
            return Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                results,
                orientation,
                layerMask,
                queryTriggerInteraction
            );
        }
    }

    public interface IPenetrationProcessor //FIXME: runner physics
    {
        /// <summary>
        ///     Compute the minimal translation required to separate the given colliders apart at specified
        ///     poses.
        /// </summary>
        /// <param name="colliderA">The first collider.</param>
        /// <param name="positionA">Position of the first collider.</param>
        /// <param name="rotationA">Rotation of the first collider.</param>
        /// <param name="colliderB">The second collider.</param>
        /// <param name="positionB">Position of the second collider.</param>
        /// <param name="rotationB">Rotation of the second collider.</param>
        /// <param name="direction">
        ///     Direction along which the translation required to separate the colliders
        ///     apart is minimal.
        /// </param>
        /// <param name="distance">
        ///     The distance along direction that is required to separate the colliders
        ///     apart.
        /// </param>
        /// <returns>True when the colliders overlap, false otherwise.</returns>
        public bool ComputePenetration(
            Collider colliderA,
            Vector3 positionA,
            Quaternion rotationA,
            Collider colliderB,
            Vector3 positionB,
            Quaternion rotationB,
            out Vector3 direction,
            out float distance
        );
    }

    public interface IAllInOnePhysicsProcessor
        : IOverlapProcessor //, IPenetrationProcessor
    { }
}
