using UnityEditor.Experimental;
using UnityEngine;

namespace RCGExtension
{
    public static class HierarchyResource
    {
        public static Color CurrentStateColor = new(0.3f, 0.7f, 0.3f, 0.2f);
        public static Color EncapsulateColor = new(0.2f, 0.6f, 0.7f, 0.2f);

        // public static string EncapsuleIcon = "📦";
        public static readonly string LockBlueIcon = "iconlockedremoteoverlay@2x.png";

        public static string FolderIconInternal
        {
            get
            {
#if UNITY_EDITOR
                return EditorResources.folderIconName;
#else
            return "" ;
#endif
            }
        }
    

    }
    public interface IHierarchyGUIPainter
    {
        bool IsDrawComponent(Component comp);
        void IconClicked(Component comp);
        string IconName { get; }
    }

    public interface IDrawHierarchyBackGround
    {
        Color BackgroundColor { get; }
        bool IsDrawGUIHierarchyBackground { get; }
    }

    public struct DetailInfo
    {
        public bool IsOutlined;
    }
    public interface IDrawDetail
    {
        bool IsFullRect { get; }
    }

    public interface IOverrideHierarchyIcon
    {
        public string IconName { get; }
        public bool IsDrawingIcon { get; }
    }

    public interface IHierarchyTimelineTrack
    {
        //這個應該要從editor code去參照...要用一個dictionary去紀錄有被timeline bind到的物件
    }
}