using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.PhysicsWrapper
{
    public class CameraDirectionRayProvider : AbstractRayProvider
    {
        public VarVector3 _cameraPositionVar;
        public VarVector3 _cameraForwardVar;

        private Vector3 cameraPosition => _cameraPositionVar != null ? _cameraPositionVar.Value :
            Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        private Vector3 cameraForward => _cameraForwardVar != null ? _cameraForwardVar.Value :
            Camera.main != null ? Camera.main.transform.forward : Vector3.forward;

        public Transform _originTransformForXZ;
        public override Ray GetRay()
        {
            //會打到自己？
            var origin = _originTransformForXZ != null
                ? _originTransformForXZ.position
                : cameraPosition;
            origin.y = cameraPosition.y;
            return new Ray(origin, cameraForward);
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
