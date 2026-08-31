using System.Collections.Generic;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MonoFSM.Core.Detection
{
    public class TriggerDetectorSource : AbstractDetectionSource
    {
        // protected override void Awake()
        // {
        //     base.Awake();
        //     if (_collider.isTrigger == false)
        //         Debug.LogError(
        //             "Collider must be set as trigger for TriggerDetectorSource to work properly.",
        //             this);
        // }
        //FIXME: 一鍵添加 Sphere Collider
        [Button]
        void AddSphereCollider()
        {
#if UNITY_EDITOR
            var col = Undo.AddComponent<SphereCollider>(gameObject);
            col.isTrigger = true;
#endif
        }

        //FIXME: 檢查是不是 trigger


        [InfoBox("Collider is Not Trigger!", InfoMessageType.Error,
            "@_collider != null && !_collider.isTrigger")]
        [Required]
        [CompRef]
        // [SerializeField]
        [Auto]
        private Collider _collider;

        // [ShowInInspector]
        // [AutoParent]
        // private Rigidbody _rigidbodyInParent;

        // [ShowIf("@_rigidbodyInParent == null")]
        [CompRef]
        [Auto]
        Rigidbody _optionalRigidbody;


        private void OnTriggerStay(Collider other)
        {
            if (IsIgnored(other))
                return;
            // 收集當前幀中仍在trigger內的collider
            _thisFrameColliders.Add(other);
        }

        public override void AfterDetection()
        {
            base.AfterDetection();
            _thisFrameColliders.Clear();
        }

        public override List<DetectionResult> GetCurrentDetections()
        {
            _buffer.Clear();
            foreach (var col in _thisFrameColliders)
                if (col != null && col.gameObject != null)
                {
                    //模擬的打擊點
                    if (IsProperCollider(col))
                    {
                        var hitPoint = col.ClosestPoint(transform.position);
                        var hitNormal = (hitPoint - col.bounds.center).normalized;
                        _buffer.Add(new DetectionResult(col.gameObject, hitPoint, hitNormal));
                    }
                    else
                        _buffer.Add(new DetectionResult(col.gameObject));
                }

            return _buffer;
        }

        bool IsProperCollider(Collider col)
        {
            // Physics.ClosestPoint can only be used with a BoxCollider, SphereCollider, CapsuleCollider and a convex MeshCollider.

            if (col is BoxCollider)
                return true;
            if (col is SphereCollider)
                return true;
            if (col is CapsuleCollider)
                return true;
            if (col is MeshCollider meshCol && meshCol.convex)
                return true;
            return false;
        }

        //FIXME: Gizmo?
        public override void EnterSceneAwake()
        {
            base.EnterSceneAwake();
            if (_collider != null)
                _collider.isTrigger = true;
        }
    }
}
