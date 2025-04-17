using UnityEngine;

namespace RCGExtension
{
    public interface IOverrideHierarchyIcon
    {
#if UNITY_EDITOR
        public string IconName { get; }
        public bool IsDrawingIcon { get; }
        public Texture2D CustomIcon { get; }
#endif
    }
}