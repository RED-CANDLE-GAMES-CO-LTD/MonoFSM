using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Runtime.LifeCycle
{
    public class MonoCullingGroup : MonoBehaviour, IResetStart
    {
        private CullingGroup _cullingGroup;
        private BoundingSphere _boundingSphere;
        private int _sphereIndex = 0;
        public float cullDistance = 50f; // Distance threshold
        public Transform target; // The target to measure distance from (e.g., Camera)
        [PreviewInInspector] private bool _isCulled = false;

        public float radius = 0.1f; // Adjustable radius for the bounding sphere
        public Vector3 gizmoOffset = Vector3.zero; // Offset for gizmo and culling sphere

        private void Start()
        {
            if (target == null)
                target = Camera.main?.transform;
            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = Camera.main;
            _boundingSphere = new BoundingSphere(transform.position + gizmoOffset, radius);
            _cullingGroup.SetBoundingSpheres(new[] { _boundingSphere });
            _cullingGroup.SetBoundingSphereCount(1);
            _cullingGroup.SetDistanceReferencePoint(target);
            _cullingGroup.SetBoundingDistances(new[] { cullDistance });
            _cullingGroup.onStateChanged = OnStateChanged;
        }

        // private void Update()
        // {
        //     // Update sphere position and radius if the object moves or radius changes
        //     _boundingSphere.position = transform.position + gizmoOffset;
        //     _boundingSphere.radius = radius;
        //     _cullingGroup.SetBoundingSpheres(new[] { _boundingSphere });
        // }

        private void OnDrawGizmosSelected()
        {
            var color = Color.yellow;
            color.a = 0.5f; // Semi-transparent
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position + gizmoOffset, radius);
        }

        private void OnStateChanged(CullingGroupEvent evt)
        {
            if (evt.hasBecomeVisible)
            {
                gameObject.SetActive(true);
                _isCulled = false;
            }
            else if (evt.hasBecomeInvisible)
            {
                gameObject.SetActive(false);
                _isCulled = true;
                // Debug.Log($"Object has been culled at distance index", this);
            }
        }

        private void OnDestroy()
        {
            if (_cullingGroup != null)
            {
                _cullingGroup.Dispose();
                _cullingGroup = null;
            }
        }

        public void ResetStart()
        {
            _cullingGroup.enabled = true;
            gameObject.SetActive(false);
        }
    }
}