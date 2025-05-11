using System;
using System.Collections.Generic;
using RCGMaker.Core.Detection;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM_Core.Runtime.Interact.SpatialDetection
{
    public class RaycastDetector:AbstractDetector
    {
        
        protected override void SetLayerOverride()
        {
            
        }

        private void Update()
        {
            PhysicsUpdate();
        }

        public void PhysicsUpdate() //network?
        {
            TryCast();
            _lastFrameColliders.AddRange(_thisFrameColliders);
            _thisFrameColliders.Clear();
        }

        void TryCast()
        {
            //rayProvider?
            var ray = _rayProvider.GetRay();
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, HittingLayer))
            {
                if (!_thisFrameColliders.Contains(hit.collider))
                {
                    OnSpatialEnter(hit.collider.gameObject);
                    _thisFrameColliders.Add(hit.collider);    
                }
            }

            foreach (var col in _lastFrameColliders)
            {
                if (!_thisFrameColliders.Contains(col))
                {
                    OnSpatialExit(col.gameObject);
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
}