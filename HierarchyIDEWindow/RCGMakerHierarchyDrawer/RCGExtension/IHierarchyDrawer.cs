using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
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
        static string lastSearchToken = "";

        public static HashSet<GameObject> _highlightedObjects = new HashSet<GameObject>();
        public static int currentIndex = 0;
        public static GameObject currentFindObject = null;
        public static void SelectCurrentObject()
        {
            if (currentFindObject == null)
            {
                return;
            }

            EditorGUIUtility.PingObject(currentFindObject);
            Selection.activeGameObject = currentFindObject;
        }
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
            Selection.activeGameObject = obj;
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

        public static void ClearFindObject()
        {
            currentFindObject = null;
            lastSearchToken = "";
            _highlightedObjects.Clear();
        }

        public static void FilterObjectsPattern(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                ClearFindObject();
                return;
            }
            term = term.ToLower();
            if (term == lastSearchToken)
                return;

            if (!term.StartsWith("t:")) return;
            term = term.Substring(2).Replace(" ", "");
            var t = AssemblyUtilities.GetTypeByCachedFullName(term); 
           
            if (t == null)
            {
                // Debug.LogError("no type match");
                return;
            }

            // Debug.Log(t);
            //FIXME: 一打開就要cache?
            
            var filteredComps = SearchForComponentType(currentPrefabComps, t);
            //get all gameobjects that have this component
            var filteredGObjs = filteredComps.Select((comp) => comp.gameObject);
            _highlightedObjects.AddRange(filteredGObjs);
        }

        public static void FindAllComponents()
        {
            
            if(PrefabStageUtility.GetCurrentPrefabStage() == null)
                return;
            //Refresh the cache if the prefab has changed
            if (currentPrefab != PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot)
            {
                currentPrefab = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                currentPrefabComps = null;
            }
            
            //fetch all components in the prefab
            if (currentPrefabComps == null)
            {
                currentPrefabComps = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot
                    .GetComponentsInChildren<Component>(true);
            }
        }

        private static GameObject currentPrefab;
        
        private static Component[] currentPrefabComps;
        public static IList<Component> SearchForComponentType(Component[] comps, System.Type type)
        {
            // Filter the list by checking if the object's name contains the search string entered by the user
            var filteredObjects = new List<Component>(); //FIXME: 可以避免GC

            foreach (var obj in comps)
            {
                // Debug.Log("lower:" + obj.name.ToLower());
                //see if type is obj or inherit
                var objT = obj.GetType();
                if (objT == type || objT.IsSubclassOf(type))
                {
                    filteredObjects.Add(obj);
                }
            }

            return filteredObjects;
            // Do something with the filtered list of objects
            // For example, you could highlight them in the scene view
        }
        public static void FilterObjects(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ClearFindObject();
                return;
            }
            token = token.ToLower();
            if (token == lastSearchToken)
                return;
            _highlightedObjects.Clear();
 
          
            // Debug.Log("SearchToken:" + searchToken);
            var allObjects = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot
                .GetComponentsInChildren<Transform>(true);
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains(token))
                {
                    _highlightedObjects.Add(obj.gameObject);
                    // Debug.Log("found object" + obj.gameObject);
                }
            }

            currentIndex = 0;
            var firstOrDefault = _highlightedObjects.FirstOrDefault();
            EditorGUIUtility.PingObject(firstOrDefault);
            currentFindObject = firstOrDefault;
            lastSearchToken = token;
            
        }
    }
}
