using System.Collections.Generic;
using System.Linq;
// using UnityEditor.Search;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    public static class FilterSearchUtility
    {
        public static IEnumerable<Component> SearchGameObjectsByTerm(List<Component> gameObjects, string term)
        {
            
            if (term == "")
                return gameObjects;
            var filteredGameObjects =
                gameObjects.Where(gObj => { return FuzzySearch.Contains(term, gObj.name); }).ToList();
            return filteredGameObjects;
        }

        public static IList<GameObject> SearchGameObjectsByTerm(List<GameObject> gameObjects, string term)
        {
            if (term == "")
                return gameObjects;
            
            var filteredGameObjects =
                gameObjects.Where(gObj => FuzzySearch.Contains(term, gObj.name)).ToList();
            return filteredGameObjects;
        }


        public static IList<Component> SearchForComponentType(List<Component> comps, string term)
        {
            // Filter the list by checking if the object's name contains the search string entered by the user
            List<Component> filteredObjects = new List<Component>();
            var termLower = term.ToLower();
            foreach (Component obj in comps)
            {
                // Debug.Log("lower:" + obj.name.ToLower());
                if (obj.GetType().Name.ToLower().Contains(termLower))
                {
                    filteredObjects.Add(obj);
                }
            }

            return filteredObjects;
            // Do something with the filtered list of objects
            // For example, you could highlight them in the scene view
        }

        public static IList<Component> SearchForComponentType(List<Component> comps, System.Type type)
        {
            // Filter the list by checking if the object's name contains the search string entered by the user
            var filteredObjects = new List<Component>();

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
    }
}