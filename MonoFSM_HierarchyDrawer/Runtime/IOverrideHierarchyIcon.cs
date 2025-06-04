using UnityEngine;

namespace RCGExtension
{
    public interface IOverrideHierarchyIcon
    {
#if UNITY_EDITOR
        public string IconName { get; }
        public bool IsDrawingIcon { get; }
        public Texture2D CustomIcon { get; }
        public bool IsPosAtHead => false;
#endif
    }

    public interface IHierarchyValueInfo
    {
        public string ValueInfo { get; }
        public bool IsDrawingValueInfo { get; }
    }
}