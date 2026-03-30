using MonoFSM.PhysicsWrapper;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    /// <summary>
    ///     Raycast 偵測器，繼承 AbstractCastCache。
    /// </summary>
    public class RaycastCache : AbstractCastCache
    {
        private IRaycastProcessor raycastProcessor =>
            _parentObj.WorldUpdateSimulator.GetCompCache<IRaycastProcessor>();

        protected override bool PerformCast(Ray ray, float distance, out RaycastHit hitInfo)
        {
            return raycastProcessor.Raycast(
                ray.origin,
                ray.direction,
                out hitInfo,
                distance,
                _hittingLayer,
                _queryTriggerInteraction
            );
        }

        protected override string DescriptionTag => "Raycast";
    }
}
