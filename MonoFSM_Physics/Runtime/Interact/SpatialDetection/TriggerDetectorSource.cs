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
    /// <summary>
    /// 用 trigger collider（同節點上的 isTrigger Collider）收集重疊物件的偵測來源，掛在 EffectDetector 底下。
    /// GameObject 固定放在 <see cref="DetectorLayer"/>（collision matrix 照它配）：Editor 下 Reset / OnValidate
    /// 發現不是就自動改掉；runtime 不改，資料要在 prefab 上就是對的。
    /// </summary>
    public class TriggerDetectorSource : AbstractDetectionSource
    {
        public override bool RequiresDetectorLayer => true;

        [ShowInInspector]
        [PropertyOrder(-100)]
        [LabelText("Layer")]
        [InfoBox("layer 固定為 Detector，由 TriggerDetectorSource 管理（Editor 下自動修正）")]
        private string LayerName => LayerMask.LayerToName(gameObject.layer);

#if UNITY_EDITOR
        private void Reset()
        {
            EnforceDetectorLayer();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (!DetectorLayer.Exists || gameObject.layer == DetectorLayer.Index) return;
            // OnValidate 當下改 GameObject 屬性可能噴「SendMessage cannot be called during ... OnValidate」，
            // 延到下一個 editor tick 再改
            EditorApplication.delayCall += EnforceDetectorLayer;
        }

        private void EnforceDetectorLayer()
        {
            if (this == null || Application.isPlaying) return;
            if (!DetectorLayer.Exists || gameObject.layer == DetectorLayer.Index) return;
            gameObject.layer = DetectorLayer.Index;
            EditorUtility.SetDirty(gameObject);
            // nested / variant 上直接寫 property 不會自己記 override
            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
                PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
        }
#endif

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
