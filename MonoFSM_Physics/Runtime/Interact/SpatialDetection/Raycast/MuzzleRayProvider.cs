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
        public Transform _muzzleTransform;
        public float _dis = 10f;
        public VarVector3 _cameraForwardVar;
        public VarVector3 _cameraPositionVar;

        Vector3 cameraRayForwardPoint
        {
            get
            {
                var origin = _cameraPositionVar.Value;
                var end = origin + _cameraForwardVar.Value * _dis;
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
