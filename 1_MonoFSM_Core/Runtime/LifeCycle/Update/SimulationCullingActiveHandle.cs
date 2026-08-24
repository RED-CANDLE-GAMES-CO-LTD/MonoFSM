using UnityEngine;

namespace MonoFSM.Culling
{
    /// <summary>
    /// Phase-specific culling handle. When this GameObject is inactive, the owning MonoObj skips
    /// BeforeSimulate, Simulate and AfterSimulate while its Render phases remain available.
    /// </summary>
    public sealed class SimulationCullingActiveHandle : MonoBehaviour
    {
    }
}
