using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Attributes
{
    //SavedFlagLink
    //GameStateSO
    //ConfigSO
    [IncludeMyAttributes]
    [InlineEditor()]
    [EditorOnly]
    public class GameStateAttribute : Attribute //好像不算state耶...StaticData?
    {
        public static string GameStateFolderPath = "10_Flags"; //TODO: 弄成一個config

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

        public static string GetFullPath(GameObject gObj, string subFolderName = "", bool isAutoGen = false)
        {
            return $"{GetRelativePath(gObj, subFolderName, isAutoGen)}/{GetFileName(gObj)}.asset";
        }
        


        public static string GetFileName(GameObject gObj)
        {
            var finalName = $"{gObj.scene.name}_{gObj.name}";
            return finalName;
        }

        public string GetRelativePath(GameObject gObj, bool isAutoGen = false)
        {
            return GetRelativePath(gObj, SubFolderName, isAutoGen);
            // var folderPath = GameStateFolderPath;
            // if (isAutoGen)
            //     folderPath += "/AutoGen";
            //
            // var sceneTokens = gObj.scene.name.Split("_");
            // var scenePrefixAct = sceneTokens.Length > 1 ? sceneTokens[0] : "Test";
            // if (SubFolderName == "")
            //     return $"{folderPath}/{scenePrefixAct}";
            // else
            //     return $"{folderPath}/{SubFolderName}/{scenePrefixAct}";
        }

        public static string GetRelativePath(GameObject gObj, string subFolderName = "", bool isAutoGen = false)
        {
            var folderPath = GameStateFolderPath;
            if (isAutoGen)
                folderPath += "/AutoGen";

            var sceneTokens = gObj.scene.name.Split("_");
            var scenePrefixAct = sceneTokens.Length > 1 ? sceneTokens[0] : "Test";
            if (subFolderName == "")
                return $"{folderPath}/{scenePrefixAct}";
            else
                return $"{folderPath}/{subFolderName}/{scenePrefixAct}";
        }

        // public static string GetPathOf(GameObject gObj, string subFolderName = "", bool isAutoGen = false)
        // {
        //     var folderPath = GameStateFolderPath;
        //     if (isAutoGen)
        //         folderPath += "/AutoGen";
        //     var sceneTokens = gObj.scene.name.Split("_");
        //     var scenePrefixAct = sceneTokens.Length > 1 ? sceneTokens[0] : "Test";
        //     var finalName = $"{gObj.scene.name}_{gObj.name}";
        //     if (subFolderName == "")
        //         return $"{folderPath}/{scenePrefixAct}/{finalName}.asset";
        //     else
        //         return $"{folderPath}/{subFolderName}/{scenePrefixAct}/{finalName}.asset";
        // }
    }
}