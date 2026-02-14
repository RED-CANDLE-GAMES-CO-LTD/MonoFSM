using System.Collections.Generic;
using MonoFSM.Core.Detection;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    public class RaycastDetectSource : AbstractDetectionSource
    {
        //FIXME: 應該用他的 layerMask
        [FormerlySerializedAs("_raycastDetector")]
        [DropDownRef]
        public RaycastCache _raycastCache;

        public override List<DetectionResult> GetCurrentDetections()
        {
            var cachedHits = _raycastCache.CachedHits;
            _buffer.Clear();
            foreach (var hit in cachedHits)
            {
                if (hit.collider == null)
                    continue;
                _buffer.Add(new DetectionResult(hit.collider.gameObject, hit.point, hit.normal));
            }

            return _buffer;
        }

        public override void UpdateDetection()
        {
            //FIXME:  應該也用queue的方式處理enter exit事件？
            //現在OnDetectEnterCheck比較係可以傳細節， Queued不行
            PhysicsUpdate();
        }

        private void PhysicsUpdate() //network?
        {
            _thisFrameColliders.Clear();
            //FIXME:  TryCast();
            //從hit拿collider
            var cachedHits = _raycastCache.CachedHits;
            if (cachedHits == null)
            {
                Debug.LogError(
                    "RaycastDetectSource: CachedHits is null. Make sure RaycastCache is properly set up.",
                    this);
                return;
            }

            foreach (var hit in cachedHits)
                _thisFrameColliders.Add(hit.collider); //這個有用嗎？
        }
    }
}
