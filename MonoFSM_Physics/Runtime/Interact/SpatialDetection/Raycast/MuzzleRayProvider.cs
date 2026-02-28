using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.PhysicsWrapper
{
    /// <summary>
    /// 從槍口發出，打向攝影機射線擊中點的射線提供者
    /// </summary>
    public class MuzzleRayProvider : AbstractRayProvider
    {
        // public VarVector3 _cameraRayHitPoint;
        public Transform _muzzleTransform;
        public float _dis = 10f;

        Vector3 cameraRayForwardPoint
        {
            get
            {
                var mainCamera = Camera.main;
                if (mainCamera == null) return Vector3.zero;
                var origin = mainCamera.transform.position;
                var end = origin + mainCamera.transform.forward * _dis;

                return end;
            }
        }

        public override Ray GetRay()
        {
            var direction = (cameraRayForwardPoint - _muzzleTransform.position).normalized;
            return new Ray(_muzzleTransform.position, direction);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_muzzleTransform == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_muzzleTransform.position, cameraRayForwardPoint);
            Gizmos.DrawSphere(cameraRayForwardPoint, 0.05f);
        }
#endif
    }
}
