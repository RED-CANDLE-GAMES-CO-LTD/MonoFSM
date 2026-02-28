using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using UnityEngine;

namespace MonoFSM.PhysicsWrapper
{
    public class CameraDirectionRayProvider : AbstractRayProvider
    {
        public Transform _originTransformForXZ;
        public override Ray GetRay()
        {
            //會打到自己？
            var mainCamera = Camera.main;
            var origin = _originTransformForXZ != null
                ? _originTransformForXZ.position
                : mainCamera.transform.position;
            origin.y = mainCamera.transform.position.y;
            //origin的位置
            return new Ray(origin, mainCamera.transform.forward);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            var origin = _originTransformForXZ != null
                ? _originTransformForXZ.position
                : mainCamera.transform.position;
            origin.y = mainCamera.transform.position.y;

            var end = origin + mainCamera.transform.forward * 5f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawSphere(origin, 0.05f);

            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(origin, "CMDir Origin");
            UnityEditor.Handles.Label(end, "CMDir Forward");
        }
#endif
    }
}
