using UnityEngine;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    public abstract class AbstractRayProvider : MonoBehaviour
    {
        public abstract Ray GetRay();
    }
}
