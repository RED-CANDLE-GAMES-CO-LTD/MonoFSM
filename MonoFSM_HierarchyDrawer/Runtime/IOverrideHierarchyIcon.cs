using UnityEngine;

namespace RCGExtension
{
    public interface IOverrideHierarchyIcon
    {
        public string IconName { get; }
        public bool IsDrawingIcon { get; }
        public Texture2D CustomIcon { get; }
    }
}