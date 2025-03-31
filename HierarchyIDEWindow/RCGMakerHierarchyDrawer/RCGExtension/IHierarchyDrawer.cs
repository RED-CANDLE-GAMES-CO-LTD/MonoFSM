using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
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
        bool IsFullRect { get; } //這要做啥？
        //
    }

    public interface IOverrideHierarchyIcon
    {
        public string IconName { get; }
        public bool IsDrawingIcon { get; }
        public Texture2D CustomIcon { get; }
    }

    public interface IHierarchyTimelineTrack
    {
        //這個應該要從editor code去參照...要用一個dictionary去紀錄有被timeline bind到的物件
    }

    public static class HierarchyHighLightEditor
    {
        public static string searchToken = "";

        public static HashSet<GameObject> _highlightedObjects = new HashSet<GameObject>();
        public static int currentIndex = 0;
        public static GameObject currentFindObject = null;

        private static GameObject FindObject(int direction)
        {
            if (_highlightedObjects.Count == 0)
            {
                return null;
            }

            currentIndex = (currentIndex + direction + _highlightedObjects.Count) % _highlightedObjects.Count;
            var enumerator = _highlightedObjects.GetEnumerator();

            for (int i = 0; i <= currentIndex; i++)
            {
                enumerator.MoveNext();
            }

            var obj = enumerator.Current;
            if (obj == null)
            {
                return null;
            }

            EditorGUIUtility.PingObject(obj);
            currentFindObject = obj;
            return enumerator.Current;
        }

        public static GameObject FindPreviousObject()
        {
            return FindObject(-1);
        }

        public static GameObject FindNextObject()
        {
            return FindObject(1);
        }

        public static void FilterObjects()
        {
            _highlightedObjects.Clear();
            if (string.IsNullOrEmpty(searchToken))
            {
                return;
            }

            searchToken = searchToken.ToLower();
            // Debug.Log("SearchToken:" + searchToken);
            var allObjects = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot
                .GetComponentsInChildren<Transform>(true);
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains(searchToken))
                {
                    _highlightedObjects.Add(obj.gameObject);
                    // Debug.Log("found object" + obj.gameObject);
                }
            }

            currentIndex = 0;
            var firstOrDefault = _highlightedObjects.FirstOrDefault();
            EditorGUIUtility.PingObject(firstOrDefault);
            currentFindObject = firstOrDefault;
            
        }
    }
}
