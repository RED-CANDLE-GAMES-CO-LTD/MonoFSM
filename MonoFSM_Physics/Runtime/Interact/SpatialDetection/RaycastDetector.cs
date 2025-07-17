using System;
using System.Collections.Generic;
using MonoFSM.Core.Simulate;
using MonoFSM.Physics;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Detection;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    public class RaycastDetector : AbstractDetector, IUpdateSimulate
    {
        public enum RaycastMode
        {
            Single, //FIXME: 應該都要用all 然後再sort, 然後會需要filter掉一部分
            All //會需要all嗎？這樣對象要全部分開？
        }

        [SerializeField] private RaycastMode _raycastMode = RaycastMode.Single;
        public float _distance = 30;
        
        private readonly List<RaycastHit> _cachedHits = new();

        private RaycastHit[] _allocHits = new RaycastHit[10]; //FIXME: 這個大小要怎麼處理？會不會有問題？ 這個是用來儲存raycast的結果

        private Collider[] _allocColliders = new Collider[10]; //FIXME: 這個大小要怎麼處理？會不會有問題？ 這個是用來儲存raycast的結果
        //用spherecast還是raycast？ spherecast會有問題嗎？

        [PreviewInInspector]
        private Collider firstHitCollider => _cachedHits.Count > 0 ? _cachedHits[0].collider : null;

        [PreviewInInspector]
        public IReadOnlyList<RaycastHit> CachedHits => _cachedHits;
        public RaycastHit CachedHit => _cachedHits.Count > 0 ? _cachedHits[0] : default;
        public Ray CachedRay => _cachedRay;

        protected override void OnDisableImplement()
        {
            // Debug.Log("RaycastDetector OnDisableImplement", this);
            _lastFrameColliders.Clear();
            _thisFrameColliders.Clear();
            _cachedHits.Clear();
            _cachedRay = default;
        }

        protected override void SetLayerOverride()
        {
        }
        // private void Update()
        // {
        //     PhysicsUpdate();
        // }

        [Auto] private IRaycastProcessor _raycastProcessor;
        [Auto] private ISphereCastProcessor _sphereCastProcessor;
        public float _sphereRadius = 0.5f; //FIXME: spherecast的半徑要怎麼處理？ 這個是用來儲存spherecast的結果
        private Ray _cachedRay;

        private void PhysicsUpdate() //network?
        {
            _thisFrameColliders.Clear();
            TryCast();
            //從hit拿collider
            foreach (var hit in _cachedHits)
            {
                if (!_lastFrameColliders.Contains(hit.collider))
                {
                    // Debug.Log("enter" + col.name, col.gameObject);
                    Debug.Log("Spatial enter: hitPoint " + hit.collider, hit.collider);

                    //Note: Detectable必須在 rigidbody上面？
                    OnDetectEnter(hit.rigidbody.gameObject, hit.point, hit.normal);
                }
            }
            // foreach (var col in _thisFrameColliders)
            //     if (!_lastFrameColliders.Contains(col))
            //     {
            //         // Debug.Log("enter" + col.name, col.gameObject);
            //         OnSpatialEnter(col.gameObject);
            //     }
                    


            foreach (var col in _lastFrameColliders)
                if (!_thisFrameColliders.Contains(col))
                {
                    //FIXME: 已經關掉的話...是不是悲劇了？ rigidbody拿不到？
                    var rb = col.attachedRigidbody;
                    if (rb == null)
                    {
                        // Debug.LogError(
                        //     "RaycastDetector: Collider has no attached Rigidbody, cannot call OnSpatialExit.", col);
                        rb = col.GetComponentInParent<Rigidbody>(true);
                        if (rb == null)
                        {
                            Debug.LogError(
                                "RaycastDetector: Collider has no attached Rigidbody or parent Rigidbody, cannot call OnSpatialExit.",
                                col);
                            continue; //跳過這個 collider
                        }
                    }

                    OnDetectExit(rb.gameObject); //gameObject錯了...哭
                }
                    

            _lastFrameColliders.Clear();
            _lastFrameColliders.AddRange(_thisFrameColliders);
            
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (_sphereCastProcessor != null)
            {
                Gizmos.DrawWireSphere(_cachedRay.origin, _sphereRadius);
                Gizmos.DrawRay(_cachedRay.origin, _cachedRay.direction * _distance);
                Gizmos.DrawWireSphere(_cachedRay.origin + _cachedRay.direction * _distance, _sphereRadius);
            }

            else
            {
                Gizmos.DrawRay(_cachedRay.origin, _cachedRay.direction * _distance);
            }
            // if (_cacehdHit.collider != null)
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawSphere(_cacehdHit.point, 0.1f);
            // }
        }

        // CameraRayProvider
        public bool _isEffectByCameraRotation;
        [SerializeField] private float _minVerticalAngle = -45f; // Minimum vertical angle limit
        [SerializeField] private float _maxVerticalAngle = 45f; // Maximum vertical angle limit
        private Transform _characterTransform; // Reference to the character's transform

        void TryCast()
        {
            var ray = _rayProvider.GetRay();
            _characterTransform = transform;
            if (_isEffectByCameraRotation && _characterTransform != null)
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    // Get camera's pitch (vertical rotation)
                    var cameraPitch = camera.transform.eulerAngles.x;
                    // Normalize angle to -180 to 180 range
                    if (cameraPitch > 180f) cameraPitch -= 360f;

                    // Clamp the pitch within our limits
                    var clampedPitch = Mathf.Clamp(cameraPitch, _minVerticalAngle, _maxVerticalAngle);

                    // Use the character's forward direction as the base
                    var characterForward = _characterTransform.forward;
                    var horizontalForward = new Vector3(characterForward.x, 0, characterForward.z).normalized;

                    // Create rotation from the character's Y rotation (yaw)
                    var characterYawRotation = Quaternion.Euler(0, _characterTransform.eulerAngles.y, 0);

                    // Apply pitch rotation around the local X axis
                    var pitchRotation = Quaternion.Euler(clampedPitch, 0, 0);

                    // First apply character's yaw, then apply the camera pitch
                    var newDirection = characterYawRotation * (pitchRotation * Vector3.forward);

                    // Create a new ray with the adjusted direction
                    ray = new Ray(ray.origin, newDirection);
                    transform.forward = newDirection;
                }
            }
            else if (_isEffectByCameraRotation)
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    // Get camera's pitch (vertical rotation)
                    var cameraPitch = camera.transform.eulerAngles.x;
                    // Normalize angle to -180 to 180 range
                    if (cameraPitch > 180f) cameraPitch -= 360f;

                    // Clamp the pitch within our limits
                    var clampedPitch = Mathf.Clamp(cameraPitch, _minVerticalAngle, _maxVerticalAngle);

                    // Default implementation when character transform is not set
                    // Create a new direction that preserves horizontal direction but applies vertical angle
                    var horizontalDir = new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z)
                        .normalized;

                    // Apply pitch rotation to the horizontal direction
                    var pitchRotation = Quaternion.Euler(clampedPitch, 0, 0);
                    var newDirection = pitchRotation * Vector3.forward;

                    // Create a new ray with the adjusted direction
                    ray = new Ray(ray.origin, newDirection);
                }
            }
            
            _cachedHits.Clear();
            _thisFrameColliders.Clear();

            _cachedRay = ray;
            if (_raycastMode == RaycastMode.Single)
            {
                if (_sphereCastProcessor != null)
                {
                    if (_sphereCastProcessor.SphereCast(ray.origin, _sphereRadius, ray.direction,
                            out var hitInfo, _distance, HittingLayer, QueryTriggerInteraction.UseGlobal))
                    {
                        if (hitInfo.distance == 0)
                            Debug.LogError(
                                "RaycastDetector: SphereCast hit distance is 0, this may indicate an issue with the ray or sphere radius.",
                                this);
                        _cachedHits.Add(hitInfo);
                        _thisFrameColliders.Add(hitInfo.collider);
                    }
                }
                else if (_raycastProcessor != null)
                {
                  
                    
                    if (_raycastProcessor.Raycast(ray.origin, ray.direction, out var hitInfo, _distance, HittingLayer))
                    {
                        //FIXME: 操作 list好嗎？
                        _cachedHits.Add(hitInfo);
                        _thisFrameColliders.Add(hitInfo.collider);
                        // Debug.Log("hit" + hit.collider.name, hit.collider);
                    }
                }
                else
                if (UnityEngine.Physics.Raycast(ray, out var hit, _distance, HittingLayer))
                {
                    _cachedHits.Add(hit);
                    _thisFrameColliders.Add(hit.collider);
                    // Debug.Log("hit" + hit.collider.name, hit.collider);
                }
            }
            // else
            // {
            //     var hits = Physics.RaycastAll(ray, _distance, HittingLayer);
            //     foreach (var h in hits)
            //     {
            //         _cachedHits.Add(h);
            //         _thisFrameColliders.Add(h.collider);
            //         Debug.Log("hit" + h.collider.name, h.collider);
            //     }
            // }
        }

        [PreviewInInspector]
        private readonly HashSet<Collider> _thisFrameColliders = new();

        [PreviewInInspector]
        private readonly HashSet<Collider> _lastFrameColliders = new(); //ondisable也要清掉？
        [Required] [Auto] [CompRef] private IRayProvider _rayProvider;
        
        //update?
        public void Simulate(float deltaTime)
        {
            PhysicsUpdate();
        }

        public void AfterUpdate()
        {
            // throw new System.NotImplementedException();
        }
        
    }

    public interface IRayProvider
    {
        Ray GetRay();
    }
   
}

