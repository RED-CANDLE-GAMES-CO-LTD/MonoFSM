using System;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using UnityEngine;

namespace MonoFSM.Physics
{
    //FIXME: 還是弄回component?
    [Serializable]
    public class CameraRayProvider : MonoBehaviour, IRayProvider
    {
        [SerializeField] private Camera _mainCamera;

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