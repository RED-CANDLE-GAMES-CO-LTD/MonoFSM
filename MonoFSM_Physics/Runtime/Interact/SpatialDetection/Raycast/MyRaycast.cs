namespace MonoFSM.Physics
{
    using UnityEngine;

    public class MyRaycast : MonoBehaviour
    {
        [Auto] private IRaycastProcessor _raycastProcessor;

        /// <summary>
        /// Casts a ray against colliders in the scene.
        /// </summary>
        /// <param name="origin">The starting point of the ray in world coordinates.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="maxDistance">The max distance the ray should check for collisions.</param>
        /// <returns>True if the ray intersects with a collider, otherwise false.</returns>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            if (_raycastProcessor != null) return _raycastProcessor.Raycast(origin, direction, maxDistance);

            // Fallback to Physics.Raycast if no processor is available
            return Physics.Raycast(origin, direction, maxDistance);
        }

        /// <summary>
        /// Casts a ray against colliders in the scene with layerMask filtering.
        /// </summary>
        /// <param name="origin">The starting point of the ray in world coordinates.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="maxDistance">The max distance the ray should check for collisions.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a ray.</param>
        /// <returns>True if the ray intersects with a collider, otherwise false.</returns>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            if (_raycastProcessor != null) return _raycastProcessor.Raycast(origin, direction, maxDistance, layerMask);

            // Fallback to Physics.Raycast if no processor is available
            return Physics.Raycast(origin, direction, maxDistance, layerMask);
        }

        /// <summary>
        /// Casts a ray against colliders in the scene and returns information on what was hit.
        /// </summary>
        /// <param name="origin">The starting point of the ray in world coordinates.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="hitInfo">If true is returned, hitInfo will contain more information about where the collider was hit.</param>
        /// <param name="maxDistance">The max distance the ray should check for collisions.</param>
        /// <returns>True if the ray intersects with a collider, otherwise false.</returns>
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            if (_raycastProcessor != null)
                return _raycastProcessor.Raycast(origin, direction, out hitInfo, maxDistance);

            // Fallback to Physics.Raycast if no processor is available
            return Physics.Raycast(origin, direction, out hitInfo, maxDistance);
        }

        /// <summary>
        /// Casts a ray against colliders in the scene and returns information on what was hit with layerMask filtering.
        /// </summary>
        /// <param name="origin">The starting point of the ray in world coordinates.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="hitInfo">If true is returned, hitInfo will contain more information about where the collider was hit.</param>
        /// <param name="maxDistance">The max distance the ray should check for collisions.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a ray.</param>
        /// <returns>True if the ray intersects with a collider, otherwise false.</returns>
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            if (_raycastProcessor != null)
                return _raycastProcessor.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);

            // Fallback to Physics.Raycast if no processor is available
            return Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);
        }

        /// <summary>
        /// Casts a ray against colliders in the scene and returns information on what was hit with layerMask and queryTriggerInteraction filtering.
        /// </summary>
        /// <param name="origin">The starting point of the ray in world coordinates.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="hitInfo">If true is returned, hitInfo will contain more information about where the collider was hit.</param>
        /// <param name="maxDistance">The max distance the ray should check for collisions.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a ray.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        /// <returns>True if the ray intersects with a collider, otherwise false.</returns>
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask,
            QueryTriggerInteraction queryTriggerInteraction)
        {
            if (_raycastProcessor != null)
                return _raycastProcessor.Raycast(origin, direction, out hitInfo, maxDistance, layerMask,
                    queryTriggerInteraction);

            // Fallback to Physics.Raycast if no processor is available
            return Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Same as Raycast but returns all hits non-alloc (without allocating memory).
        /// </summary>
        /// <param name="origin">The starting point of the ray in world coordinates.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="results">The buffer to store the results in.</param>
        /// <param name="maxDistance">The max distance the ray should check for collisions.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a ray.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        /// <returns>The number of hits stored in the results buffer.</returns>
        public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance,
            int layerMask,
            QueryTriggerInteraction queryTriggerInteraction)
        {
            if (_raycastProcessor != null)
                return _raycastProcessor.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask,
                    queryTriggerInteraction);

            // Fallback to Physics.RaycastNonAlloc if no processor is available
            return Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }
    }
}