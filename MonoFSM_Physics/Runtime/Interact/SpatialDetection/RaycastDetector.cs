using System;
using System.Collections.Generic;
using MonoFSM_Core.Simulate;
using MonoFSM.Physics;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.Detection;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM_Core.Runtime.Interact.SpatialDetection
{
    public class RaycastDetector : AbstractDetector, IUpdateSimulate
    {
        public enum RaycastMode
        {
            Single,
            All
        }

        [SerializeField] private RaycastMode _raycastMode = RaycastMode.Single;
        public float _distance = 30;
        
        private readonly List<RaycastHit> _cachedHits = new();

        [PreviewInInspector]
        private Collider firstHitCollider => _cachedHits.Count > 0 ? _cachedHits[0].collider : null;

        [PreviewInInspector]
        public IReadOnlyList<RaycastHit> CachedHits => _cachedHits;
        public RaycastHit CachedHit => _cachedHits.Count > 0 ? _cachedHits[0] : default;
        public Ray CachedRay => _cachedRay;
        protected override void SetLayerOverride()
        {
        }
        // private void Update()
        // {
        //     PhysicsUpdate();
        // }

        [Auto] private IRaycastProcessor _raycastProcessor;
        private Ray _cachedRay;
        public void PhysicsUpdate() //network?
        {
            _thisFrameColliders.Clear();
            TryCast();
            foreach (var col in _thisFrameColliders)
                if (!_lastFrameColliders.Contains(col))
                    // Debug.Log("enter" + col.name, col.gameObject);
                    OnSpatialEnter(col.gameObject);


            foreach (var col in _lastFrameColliders)
                if (!_thisFrameColliders.Contains(col))
                    OnSpatialExit(col.gameObject);

            _lastFrameColliders.Clear();
            _lastFrameColliders.AddRange(_thisFrameColliders);
            
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_cachedRay.origin, _cachedRay.direction * _distance);
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
                if (_raycastProcessor != null)
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
                if (Physics.Raycast(ray, out var hit, _distance, HittingLayer))
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

        private readonly HashSet<Collider> _thisFrameColliders = new();
        private readonly HashSet<Collider> _lastFrameColliders = new();
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

