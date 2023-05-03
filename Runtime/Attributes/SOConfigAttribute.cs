using System;
using UnityEngine;

namespace RCGMaker.Core.Attributes
{
    public class SOConfigAttribute : Attribute
    {
        public string SubFolderPath = "";
        public string PostProcessMethodName = "";

        public SOConfigAttribute(string subFolderPath, string PostProcessMethodName = "")
        {
            SubFolderPath = subFolderPath;
            this.PostProcessMethodName = PostProcessMethodName;
        }

        public string GetPathFromOwnerObj(GameObject gObj, string configName)
        {
            var finalName = $"{gObj.name}_{configName}";
            if (SubFolderPath == "")
                return $"{finalName}.asset";
            else
                return $"{SubFolderPath}/{finalName}.asset";
        }
    }
}