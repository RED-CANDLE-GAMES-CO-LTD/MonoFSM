using System;
using System.Text.RegularExpressions;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Variable
{
    [Serializable]
    public class PropertyOf
    {
        public PropertyTag _varTag;

        public PropertyOf _nextLevel;
        //nested? array?
    }

    //a.b
    public class PropertyTag : ScriptableObject, IStringKey
    {
        //scriptable object會殘留？
        [NonSerialized] private string _cachedStringKey;

        [PreviewInInspector]
        public string GetStringKey
        {
            get
            {
                //remove Characters between '[' and ']'

                _cachedStringKey = Regex.Replace(name, @"\[.*?\]", string.Empty);
                _cachedStringKey = Regex.Replace(_cachedStringKey, @"\s+", string.Empty);
                // _cachedStringKey = name.Replace(" ", "");
                return _cachedStringKey;
            }
        }
    }
}