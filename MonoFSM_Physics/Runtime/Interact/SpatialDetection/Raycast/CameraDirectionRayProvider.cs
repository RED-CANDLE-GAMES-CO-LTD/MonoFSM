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

        /// <summary>
        /// 沿 cameraForward 射線，找到 XZ 對齊 _originTransformForXZ 的點作為 origin。
        /// 若未設定 _originTransformForXZ 則直接使用 cameraPosition。
        /// </summary>
        private Vector3 GetOrigin(Vector3 camPos, Vector3 camFwd)
        {
            if (_originTransformForXZ == null) return camPos;

            var targetPos = _originTransformForXZ.position;
            // 用 X 或 Z 分量中較大的那個求參數 t，避免除以接近 0 的值
            float t;
            if (Mathf.Abs(camFwd.x) >= Mathf.Abs(camFwd.z))
                t = Mathf.Abs(camFwd.x) > 1e-6f ? (targetPos.x - camPos.x) / camFwd.x : 0f;
            else
                t = Mathf.Abs(camFwd.z) > 1e-6f ? (targetPos.z - camPos.z) / camFwd.z : 0f;

            return new Vector3(targetPos.x, camPos.y + t * camFwd.y, targetPos.z);
        }

        public override Ray GetRay()
        {
            //會打到自己？
            var camFwd = cameraForward;
            return new Ray(GetOrigin(cameraPosition, camFwd), camFwd);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var camFwd = cameraForward;
            var origin = GetOrigin(cameraPosition, camFwd);

            var end = origin + cameraForward * 5f;

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
