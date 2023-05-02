using System;
using UnityEngine;

namespace RCGMaker.Core.Attributes
{
    //SavedFlagLink
    //GameStateSO
    //ConfigSO
    public class GameStateAttribute : Attribute
    {
        public static string FlagFolderPath = "10_Flags"; //TODO: 弄成一個config

        public GameStateAttribute()
        {
        }

        //FlagFolderPath + SubFolderName + sceneName+Position + flagName
        public GameStateAttribute(string subFolderName)
        {
            this.SubFolderName = subFolderName;
            // this.FlagName = flagName;
        }

        //TODO: local variable? 放在prefab旁邊嗎？ 也怪怪的 local就不需要了


        public string SubFolderName = "";
        // public string FlagName = "DefaultFlagName"; //FIXME: 這個要自動抓？
        //
        // public string GetPath()
        // {
        //     if (SubFolderName == "")
        //         return $"{FlagFolderPath}/{FlagName}.asset";
        //     else
        //         return $"{FlagFolderPath}/{SubFolderName}/{FlagName}.asset";
        // }

        public string GetPath(GameObject gObj)
        {
            var scenePrefixAct = gObj.scene.name.Split("_")[0];
            var finalName = $"{gObj.scene.name}_{gObj.transform.position}_{gObj.name}";
            if (SubFolderName == "")
                return $"{FlagFolderPath}/{scenePrefixAct}/{finalName}.asset";
            else
                return $"{FlagFolderPath}/{SubFolderName}/{scenePrefixAct}/{finalName}.asset";
        }
    }
}