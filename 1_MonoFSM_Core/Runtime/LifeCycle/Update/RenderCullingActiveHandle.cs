using UnityEngine;

namespace MonoFSM.Culling
{
    /// <summary>
    /// Phase-specific culling handle. When this GameObject is inactive, the owning MonoObj skips
    /// Render and AfterRender while its simulation phases remain available.
    /// </summary>
    public sealed class RenderCullingActiveHandle : MonoBehaviour
    {
    }
}
