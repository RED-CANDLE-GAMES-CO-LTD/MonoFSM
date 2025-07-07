using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.LifeCycle
{
    [Obsolete]
    public class MonoCullingGroup : MonoBehaviour, IResetStart
    {
        public GameObject _overrideTarget;
        private CullingGroup _cullingGroup;
        private BoundingSphere _boundingSphere;
        private int _sphereIndex = 0;
        private static readonly float CullDistance = 10f; // Distance threshold

        // public Transform target; // The target to measure distance from (e.g., Camera)
        public Transform trackingObserver;
        [PreviewInInspector] private bool _isCulled = false;

        public float radius = 0.1f; // Adjustable radius for the bounding sphere
        public Vector3 gizmoOffset = Vector3.zero; // Offset for gizmo and culling sphere

        private void Start()
        {
            // if (target == null)
            var target = Camera.main?.transform;
            trackingObserver ??= transform;
            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = Camera.main;
            _boundingSphere = new BoundingSphere(trackingObserver.position + gizmoOffset, radius);
            _boundingSpheres[0] = _boundingSphere;
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(1);
            _cullingGroup.SetDistanceReferencePoint(target);
            _cullingGroup.SetBoundingDistances(new[] { CullDistance });
            _cullingGroup.onStateChanged = OnStateChanged;
            _cullingGroup.enabled = true;
            if (gameObject.isStatic || trackingObserver == null)
                enabled = false; // Disable if the object is static, as it won't need update
        }

        private BoundingSphere[] _boundingSpheres = new BoundingSphere[1];

        private void Update()
        {
            _boundingSpheres[0].position = trackingObserver.position + gizmoOffset;
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
                HasBecomeVisible();
            }
            else if (evt.hasBecomeInvisible)
            {
                HasBecomeInvisible();
            }
        }

        [SerializeField]
        [CompRef]
        [AutoChildren]
        private OnCullingVisibleHandler _onCullingVisibleHandler;

        private void HasBecomeVisible()
        {
            // Override this method to handle when the object becomes visible
            //GPUI的話是關掉一部分，
            if (_overrideTarget != null)
                _overrideTarget.SetActive(true);
            else
                gameObject.SetActive(true);
            _onCullingVisibleHandler?.OnVisible(); //用裝的
            _isCulled = false;
        }

        private void HasBecomeInvisible()
        {
            // Override this method to handle when the object becomes invisible
            if (_overrideTarget != null)
                _overrideTarget.SetActive(false);
            else
                gameObject.SetActive(false);
            _onCullingVisibleHandler?.OnInvisible();
            _isCulled = true;
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