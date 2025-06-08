using UnityEngine;

namespace RCGExtension
{
    //FIXME: 這個獨立抽出來有什麼差？interface還不是被引用了
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
                return UnityEditor.Experimental.EditorResources.folderIconName;
#else
            return "" ;
#endif
            }
        }
    }


    /// <summary>
    /// FIXME: 要把這個撿回來做嗎？ 動畫編輯輔助
    /// </summary>
    public interface IHierarchyGUIPainter
    {
        bool IsDrawComponent(Component comp);
        void IconClicked(Component comp);
        string IconName { get; }
    }

    public interface IHierarchyButton
    {
        bool IsDrawButton { get; }
        string IconName { get; }
        void OnClick();
    }

    public interface IDrawHierarchyBackGround
    {
#if UNITY_EDITOR
        Color BackgroundColor { get; }
        bool IsDrawGUIHierarchyBackground { get; }
#endif
    }

    public struct DetailInfo
    {
        public bool IsOutlined;
    }

    public interface IDrawDetail
    {
        bool IsFullRect { get; } //這要做啥？
        //
    }


    public interface IHierarchyTimelineTrack
    {
        //這個應該要從editor code去參照...要用一個dictionary去紀錄有被timeline bind到的物件
    }
}