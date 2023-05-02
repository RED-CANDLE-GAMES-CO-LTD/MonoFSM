using System;
using UnityEngine;

namespace RCGMaker.Core.Attributes
{
    public class ConfigSOAttribute : Attribute
    {
        public string SubFolderPath = "";

        public ConfigSOAttribute(string subFolderPath)
        {
            SubFolderPath = subFolderPath;
        }

        public string GetPathFromOwnerObj(GameObject gObj)
        {
            var finalName = $"{gObj.scene.name}_{gObj.transform.position}_{gObj.name}";
            if (SubFolderPath == "")
                return $"{finalName}.asset";
            else
                return $"{SubFolderPath}/{finalName}.asset";
        }
    }
}