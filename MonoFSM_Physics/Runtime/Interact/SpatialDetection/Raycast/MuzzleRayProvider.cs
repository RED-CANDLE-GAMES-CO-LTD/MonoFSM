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
        public VarVector3 _cameraHitPos;
        Vector3 GetTargetPoint()
        {
            if (_cameraHitPos != null)
                return _cameraHitPos.Value;

            var origin = _cameraPositionVar.Value;
            return origin + _cameraForwardVar.Value * _dis;
        }

        public override Ray GetRay()
        {
            var direction = (GetTargetPoint() - _muzzleTransform.position).normalized;
            return new Ray(_muzzleTransform.position, direction);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_muzzleTransform == null) return;

            var muzzlePos = _muzzleTransform.position;
            var targetPoint = GetTargetPoint();

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(muzzlePos, targetPoint);
            Gizmos.DrawSphere(targetPoint, 0.05f);

            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(muzzlePos, "Muzzle Origin");
            UnityEditor.Handles.Label(targetPoint, _cameraHitPos != null ? "Muzzle Target (Hit)" : "Muzzle Target (Fallback)");
        }
#endif
    }
}
