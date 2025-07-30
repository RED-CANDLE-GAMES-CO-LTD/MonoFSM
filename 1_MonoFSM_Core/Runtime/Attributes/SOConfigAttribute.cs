using System;
using _1_MonoFSM_Core.Runtime.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Attributes
{
    [EditorOnly]
    [IncludeMyAttributes]
    [TypeRestrictFilter] //FIXME: 好像要可以過濾對應的型別, list失敗
    [ListDrawerSettings(ShowFoldout = false)]
    public class SOConfigAttribute : Attribute
    {
        public string SubFolderPath = "";
        public string PostProcessMethodName = "";

        //FIXME: 空路徑，就放在原本同一個資料夾？
        public SOConfigAttribute(string subFolderPath, string PostProcessMethodName = "")
        {
            SubFolderPath = subFolderPath;
            this.PostProcessMethodName = PostProcessMethodName;
        }

        public string GetPathFromOwnerObj(GameObject gObj, string configName)
        {
            //命名
            var finalName = $"{gObj.name}";
            if (SubFolderPath == "")
                return $"{finalName}.asset";
            else
                return $"{SubFolderPath}/{finalName}.asset";
        }

        public string GetFilePath(string configName)
        {
            var finalName = $"{configName}";
            if (SubFolderPath == "")
                return $"{finalName}.asset";
            else
                return $"{SubFolderPath}/{finalName}.asset";
        }
    }
}