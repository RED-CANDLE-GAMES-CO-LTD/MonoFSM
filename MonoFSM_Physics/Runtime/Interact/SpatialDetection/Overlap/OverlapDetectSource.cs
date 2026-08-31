using System.Collections.Generic;
using MonoFSM.Core.Detection;
using MonoFSM.PhysicsWrapper;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    public class OverlapDetectSource : AbstractDetectionSource
    {
        [FormerlySerializedAs("_myOverlap")]
        // [DropDownRef]
        [CompRef]
        public MyOverlap _overlapProcessor; //FIXME: 應該用全域的？同個環境應該只會用有一種方式？

        [Header("Overlap Settings")]
        [InfoBox(
            "會優先使用同 GameObject 上的 Collider 參數，如果沒有找到才使用手動設定",
            InfoMessageType.Info
        )]
        public OverlapShape _overlapShape = OverlapShape.Sphere;

        [Header("Manual Override (當沒有對應 Collider 時使用)")]
        [ShowIf("@_overlapShape == OverlapShape.Sphere")]
        public float _radius = 1f;

        [ShowIf("@_overlapShape == OverlapShape.Box")]
        public Vector3 _halfExtents = Vector3.one * 0.5f;

        [ShowIf("@_overlapShape == OverlapShape.Capsule")]
        public float _capsuleRadius = 0.5f;

        [ShowIf("@_overlapShape == OverlapShape.Capsule")]
        public float _capsuleHeight = 2f;

        // Cached Colliders
        [Auto]
        private SphereCollider _sphereCollider;

        [Auto]
        private BoxCollider _boxCollider;

        [Auto]
        private CapsuleCollider _capsuleCollider;

        public LayerMask _layerMask = -1;
        public QueryTriggerInteraction _queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

        [ShowInInspector]
        private readonly Collider[] _overlapResults = new Collider[64];

        // private void CacheColliders()
        // {
        //     _sphereCollider = GetComponent<SphereCollider>();
        //     _boxCollider = GetComponent<BoxCollider>();
        //     _capsuleCollider = GetComponent<CapsuleCollider>();
        // }

        //Collider 的 size/radius 是 local 的，要乘上 lossyScale 才會跟 trigger 判定的範圍一致
        private float MaxScale
        {
            get
            {
                var s = transform.lossyScale;
                return Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
            }
        }

        //Collider 的 center 是 local offset，OverlapXXX 要世界座標
        private Vector3 GetActualCenter()
        {
            if (_sphereCollider != null && _overlapShape == OverlapShape.Sphere)
                return transform.TransformPoint(_sphereCollider.center);
            if (_boxCollider != null && _overlapShape == OverlapShape.Box)
                return transform.TransformPoint(_boxCollider.center);
            if (_capsuleCollider != null && _overlapShape == OverlapShape.Capsule)
                return transform.TransformPoint(_capsuleCollider.center);
            return transform.position;
        }

        private float GetActualRadius()
        {
            return _sphereCollider != null ? _sphereCollider.radius * MaxScale : _radius;
        }

        private Vector3 GetActualHalfExtents()
        {
            if (_boxCollider == null)
                return _halfExtents;

            var s = transform.lossyScale;
            var half = _boxCollider.size * 0.5f;
            return new Vector3(
                half.x * Mathf.Abs(s.x),
                half.y * Mathf.Abs(s.y),
                half.z * Mathf.Abs(s.z)
            );
        }

        private float GetActualCapsuleRadius()
        {
            return _capsuleCollider != null ? _capsuleCollider.radius * MaxScale : _capsuleRadius;
        }

        private float GetActualCapsuleHeight()
        {
            return _capsuleCollider != null ? _capsuleCollider.height * MaxScale : _capsuleHeight;
        }

        public enum OverlapShape
        {
            Sphere,
            Box,
            Capsule,
        }

        //單純回傳結果
        public override List<DetectionResult> GetCurrentDetections()
        {
            _buffer.Clear();
            if (_overlapProcessor == null)
            {
                Debug.LogWarning(
                    "[OverlapDetectSource] _overlapProcessor is null，需要同物件上的 MyOverlap",
                    this
                );
                return _buffer;
            }

            foreach (var col in _thisFrameColliders)
            {
                if (col == null)
                {
                    Debug.LogError("[OverlapDetectSource] Collider is null, skipping...", this);
                    continue;
                }

                //跟 TriggerDetectorSource 一致：用 collider 自己的 GameObject。
                //TriggerDetectableTarget 是掛在各個 collider 節點上，不是掛在 Rigidbody 節點上，
                //抓 attachedRigidbody.gameObject 會找不到 BaseEffectDetectTarget 而整個判定失效。
                //ClosestPoint 只能用在 Box/Sphere/Capsule/convex Mesh，其他型別只回報命中不給 hitPoint
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

        private static bool IsProperCollider(Collider col)
        {
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

        public override void UpdateDetection()
        {
            PhysicsUpdate();
        }

        private void PhysicsUpdate()
        {
            _thisFrameColliders.Clear();

            if (_overlapProcessor == null)
            {
                Debug.LogWarning(
                    "[OverlapDetectSource] _overlapProcessor is null，需要同物件上的 MyOverlap",
                    this
                );
                return;
            }

            _hitCount = PerformOverlap();
            if (_hitCount >= _overlapResults.Length)
                Debug.LogWarning(
                    $"[OverlapDetectSource] overlap buffer 滿了({_overlapResults.Length})，可能漏偵測，考慮縮小範圍或設 _layerMask",
                    this
                );

            // 收集這一frame的colliders
            for (var i = 0; i < _hitCount; i++)
            {
                if (IsIgnored(_overlapResults[i]))
                    continue;
                _thisFrameColliders.Add(_overlapResults[i]);
            }

            // 檢查進入事件：上個frame不在，這個frame在
            // foreach (var col in _thisFrameColliders)
            //     if (!_lastFrameColliders.Contains(col))
            //     {
            //         var targetObject = col.attachedRigidbody
            //             ? col.attachedRigidbody.gameObject
            //             : col.gameObject;
            //
            //         var hitPoint = col.bounds.center;
            //         _detector.OnDetectEnterCheck(targetObject, hitPoint);
            //     }
            //
            // // 檢查離開事件：上個frame在，這個frame不在
            // foreach (var col in _lastFrameColliders)
            //     if (!_thisFrameColliders.Contains(col))
            //     {
            //         var rb = col.attachedRigidbody;
            //         if (rb == null)
            //         {
            //             rb = col.GetComponentInParent<Rigidbody>(true);
            //             if (rb == null)
            //                 continue; // 跳過這個 collider
            //         }
            //
            //         _detector.OnDetectExitCheck(rb.gameObject);
            //     }

            // 更新 frame 記錄
            // _lastFrameColliders.Clear();
            // _lastFrameColliders.AddRange(_thisFrameColliders);
        }

        int _hitCount = 0;

        private int PerformOverlap()
        {
            var position = GetActualCenter();

            switch (_overlapShape)
            {
                case OverlapShape.Sphere:
                    return _overlapProcessor.OverlapSphereNonAlloc(
                        position,
                        GetActualRadius(),
                        _overlapResults,
                        _layerMask,
                        _queryTriggerInteraction
                    );

                case OverlapShape.Box:
                    return _overlapProcessor.OverlapBoxNonAlloc(
                        position,
                        GetActualHalfExtents(),
                        _overlapResults,
                        transform.rotation,
                        _layerMask,
                        _queryTriggerInteraction
                    );

                case OverlapShape.Capsule:
                    var actualRadius = GetActualCapsuleRadius();
                    var actualHeight = GetActualCapsuleHeight();
                    return _overlapProcessor.OverlapCapsuleNonAlloc(
                        position + Vector3.up * (actualHeight * 0.5f - actualRadius),
                        position + Vector3.down * (actualHeight * 0.5f - actualRadius),
                        actualRadius,
                        _overlapResults,
                        _layerMask,
                        _queryTriggerInteraction
                    );

                default:
                    return 0;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            var position = GetActualCenter();

            switch (_overlapShape)
            {
                case OverlapShape.Sphere:
                    Gizmos.DrawWireSphere(position, GetActualRadius());
                    break;

                case OverlapShape.Box:
                    var actualHalfExtents = GetActualHalfExtents();
                    Gizmos.matrix = Matrix4x4.TRS(position, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, actualHalfExtents * 2);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;

                case OverlapShape.Capsule:
                    var actualRadius = GetActualCapsuleRadius();
                    var actualHeight = GetActualCapsuleHeight();
                    // 簡化的膠囊繪製
                    var point1 = position + Vector3.up * (actualHeight * 0.5f - actualRadius);
                    var point2 = position + Vector3.down * (actualHeight * 0.5f - actualRadius);
                    Gizmos.DrawWireSphere(point1, actualRadius);
                    Gizmos.DrawWireSphere(point2, actualRadius);
                    break;
            }
        }
    }
}
