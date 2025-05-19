using System.Collections.Generic;
using RCGMaker.Core.Detection;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM_Core.Runtime.Interact.SpatialDetection
{
    public class RaycastDetector:AbstractDetector
    {
        public enum RaycastMode
        {
            Single,
            All
        }

        [SerializeField] private RaycastMode _raycastMode = RaycastMode.Single;
        public float _distance = 30;
        private readonly List<RaycastHit> _cachedHits = new();
        public IReadOnlyList<RaycastHit> CachedHits => _cachedHits;
        public RaycastHit CachedHit => _cachedHits.Count > 0 ? _cachedHits[0] : default;

        protected override void SetLayerOverride()
        {
            
        }

        private void Update()
        {
            PhysicsUpdate();
        }

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
            Gizmos.DrawRay(_rayProvider.GetRay().origin, _rayProvider.GetRay().direction * _distance);
            // if (_cacehdHit.collider != null)
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawSphere(_cacehdHit.point, 0.1f);
            // }
        }

        void TryCast()
        {
            var ray = _rayProvider.GetRay();
            _cachedHits.Clear();
            _thisFrameColliders.Clear();
            if (_raycastMode == RaycastMode.Single)
            {
                if (Physics.Raycast(ray, out var hit, _distance, HittingLayer))
                {
                    _cachedHits.Add(hit);
                    _thisFrameColliders.Add(hit.collider);
                    // Debug.Log("hit" + hit.collider.name, hit.collider);
                }
            }
            else
            {
                var hits = Physics.RaycastAll(ray, _distance, HittingLayer);
                foreach (var h in hits)
                {
                    _cachedHits.Add(h);
                    _thisFrameColliders.Add(h.collider);
                    Debug.Log("hit" + h.collider.name, h.collider);
                }
            }
        }

        private readonly HashSet<Collider> _thisFrameColliders = new();
        private readonly HashSet<Collider> _lastFrameColliders = new();
        [SerializeReference] public IRayProvider _rayProvider;
        
        //update?
    }

    public interface IRayProvider
    {
        Ray GetRay();
    }
    
    public class CameraRayProvider:IRayProvider
    {
        [SerializeField] Camera _mainCamera;
        public Ray GetRay()
        {
            var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            if (_mainCamera == null) _mainCamera = Camera.main;
            // Create ray from camera through screen center
            var ray = _mainCamera.ScreenPointToRay(screenCenter);
            return ray;
        }
    }
    
    public class TransformForwardRayProvider:IRayProvider
    {
        [SerializeField] Transform _transform;
        public Ray GetRay()
        {
            // Create ray from camera through screen center
            var ray = new Ray(_transform.position, _transform.forward);
            return ray;
        }
    }
}

