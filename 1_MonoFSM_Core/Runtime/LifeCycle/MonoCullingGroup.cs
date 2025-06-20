using System;
using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Runtime.LifeCycle
{
    public class MonoCullingGroup : MonoBehaviour, IResetStart
    {
        private void OnBecameVisible()
        {
            throw new NotImplementedException();
        }

        private CullingGroup _cullingGroup;
        private BoundingSphere _boundingSphere;
        private int _sphereIndex = 0;
        public float cullDistance = 50f; // Distance threshold

        // public Transform target; // The target to measure distance from (e.g., Camera)
        public Transform trackingObject;
        [PreviewInInspector] private bool _isCulled = false;

        public float radius = 0.1f; // Adjustable radius for the bounding sphere
        public Vector3 gizmoOffset = Vector3.zero; // Offset for gizmo and culling sphere

        private void Start()
        {
            // if (target == null)
            var target = Camera.main?.transform;
            trackingObject ??= transform;
            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = Camera.main;
            _boundingSphere = new BoundingSphere(trackingObject.position + gizmoOffset, radius);
            _boundingSpheres[0] = _boundingSphere;
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(1);
            _cullingGroup.SetDistanceReferencePoint(target);
            _cullingGroup.SetBoundingDistances(new[] { cullDistance });
            _cullingGroup.onStateChanged = OnStateChanged;
            _cullingGroup.enabled = true;
            if (gameObject.isStatic || trackingObject == null)
                enabled = false; // Disable if the object is static, as it won't need update
        }

        private BoundingSphere[] _boundingSpheres = new BoundingSphere[1];

        private void Update()
        {
            _boundingSpheres[0].position = trackingObject.position + gizmoOffset;
            // _cullingGroup.SetBoundingSpheres(_boundingSpheres);
        }

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