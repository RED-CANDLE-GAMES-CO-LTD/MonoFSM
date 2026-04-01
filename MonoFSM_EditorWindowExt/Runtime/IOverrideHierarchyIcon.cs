using UnityEngine;

namespace MonoFSM.EditorExtension
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
#if UNITY_EDITOR
        public string ValueInfo { get; }
        public bool IsDrawingValueInfo { get; }
        public string Note => "";
#endif
    }
}
