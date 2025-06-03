using UnityEngine;

namespace Gizmo
{
    public class LineGizmoNode : MonoBehaviour
    {
        public Vector3 offset;
        public Color color = Color.red;

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawLine(transform.position, transform.position + offset);
        }
    }
}