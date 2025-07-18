using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HierarchyIDEWindow.MonoFSM_HierarchyDrawer.Editor
{
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

        private static void FilterObjectsPattern(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                ClearFindObject();
                return;
            }
            term = term.ToLower();
            if (term == lastSearchToken)
                return;

            // Debug.Log("SearchToken:" + term);

            //用t:開頭的字串，表示要搜尋特定類型的MonoBehaviour
            if (term.StartsWith("t:"))
                term = term.Substring(2).Replace(" ", "");

            //完全符合的也可以試試看
            var t = TypeFinderUtility.FindMonoBehaviourType(term, true);
            //FIXME: 可能搜到多個？
            // var t = AssemblyUtilities.GetTypeByCachedFullName(term); 
            // Debug.Log("Found Type:" + t);
            if (t == null)
            {
                
                return;
            }

            // Debug.Log("Searching for type: " + t.FullName);
            //FIXME: 一打開就要cache?
            FindAllComponentsInPrefab();
            var filteredComps = SearchForComponentType(currentPrefabComps, t);
            if (filteredComps == null)
            {
                Debug.LogError("Null: Searching for type: " + t.FullName);
                return;
            }
                
            //get all gameobjects that have this component
            var filteredGObjs = filteredComps.Select((comp) => comp.gameObject);
            // Debug.Log(filteredGObjs.Count().ToString());
            _highlightedObjects.AddRange(filteredGObjs);
        }

        public static void FindAllComponentsInPrefab()
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
            // if (currentPrefabComps == null)
            // {
            if (currentPrefabComps == null)
                currentPrefabComps = new List<Component>();
            currentPrefabComps.Clear();
            PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot
                .GetComponentsInChildren(true, currentPrefabComps);
            // Debug.Log("Length: " + currentPrefabComps.Count);
            // }
        }

        private static GameObject currentPrefab;

        private static List<Component> currentPrefabComps = new();

        public static IList<Component> SearchForComponentType(List<Component> comps, System.Type type)
        {
            // Filter the list by checking if the object's name contains the search string entered by the user
            var filteredObjects = new List<Component>(); //FIXME: 可以避免GC

            foreach (var obj in comps)
            {
                // Debug.Log("lower:" + obj.name.ToLower());
                //see if type is obj or inherit
                if (obj == null)
                    continue;
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

            FilterObjectsPattern(token);

            currentIndex = 0;
            var firstOrDefault = _highlightedObjects.FirstOrDefault();
            EditorGUIUtility.PingObject(firstOrDefault);
            currentFindObject = firstOrDefault;
            lastSearchToken = token;
            
        }
    }
}