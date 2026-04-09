using System.Collections.Generic;
using MonoFSM.Core.Detection;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    public class CastDetectSource : AbstractDetectionSource
    {
        [FormerlySerializedAs("_raycastDetector")]
        [FormerlySerializedAs("_raycastCache")]
        [DropDownRef]
        public AbstractCastCache _castCache;

        public bool _onlyDetectFirst = true;

        public override List<DetectionResult> GetCurrentDetections()
        {
            var cachedHits = _castCache.CachedHits;
            _buffer.Clear();
            if (_onlyDetectFirst && cachedHits.Count > 0)
            {
                _buffer.Add(new DetectionResult(cachedHits[0].collider.gameObject,
                    cachedHits[0].point, cachedHits[0].normal));
                return _buffer;
            }
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
            PhysicsUpdate();
        }

        private void PhysicsUpdate()
        {
            _thisFrameColliders.Clear();
            var cachedHits = _castCache.CachedHits;
            if (cachedHits == null)
            {
                Debug.LogError(
                    "CastDetectSource: CachedHits is null. Make sure CastCache is properly set up.",
                    this);
                return;
            }

            foreach (var hit in cachedHits)
                _thisFrameColliders.Add(hit.collider);
        }
    }
}
