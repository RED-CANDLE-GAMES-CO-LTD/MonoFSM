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

        protected override int PerformCast(Ray ray, float distance, RaycastHit[] results)
        {
            return raycastProcessor.RaycastNonAlloc(
                ray.origin,
                ray.direction,
                results,
                distance,
                _hittingLayer,
                _queryTriggerInteraction
            );
        }

        protected override string DescriptionTag => "Raycast";
    }
}
