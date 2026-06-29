using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.PhysicsWrapper
{
    /// <summary>
    /// 從槍口發出，打向攝影機射線擊中點的射線提供者
    /// fixme: 不是很對
    /// </summary>
    public class MuzzleRayProvider : AbstractRayProvider
    {
        public VarTransformWrapper _muzzleTransform;
        public float _dis = 10f; //FIXME: dis很怪？Offset?
        public VarVector3 _cameraForwardVar; //camera方向
        public VarVector3 _cameraPositionVar; //camera在哪

        public VarVector3
            _cameraHitPos; //camera射線現在打中哪裡 fixme: 這個現在最大！ muzzle如果會跟著角色旋轉位置就會跑掉(除非用aimmode)
        Vector3 GetTargetPoint()
        {
            if (_cameraHitPos != null)
                return _cameraHitPos.Value;

            var origin = _cameraPositionVar.Value;
            return origin + _cameraForwardVar.Value * _dis;
        }

        public override Ray GetRay()
        {
            var direction = (GetTargetPoint() - _muzzleTransform.Value.position).normalized;
            return new Ray(_muzzleTransform.Value.position, direction);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_muzzleTransform == null) return;

            var muzzlePos = _muzzleTransform.Value?.position ?? Vector3.zero;
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
