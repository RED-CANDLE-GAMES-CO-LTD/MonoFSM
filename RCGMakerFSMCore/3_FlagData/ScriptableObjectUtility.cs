using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public static class ScriptableHelper
{
    public static void SetDirty(Object obj)
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(obj);
#endif
    }

#if UNITY_EDITOR
    //在限定資料夾裡面找檔案
    public static List<T> FindAllSO<T>(Object collection, string overrideTypeName = "") where T : Object
    {
        var soList = new List<T>();
        var myPath = AssetDatabase.GetAssetPath(collection);
        var soType = typeof(T);
        var typeName = !string.IsNullOrEmpty(overrideTypeName) ? overrideTypeName : soType.Name;

        // Debug.Log("Mypath" + name + ":" + myPath);
        var dirPath = System.IO.Path.GetDirectoryName(myPath);
        Debug.Log("Mypath dir" + collection.name + ":" + dirPath);
        var allSOs = AssetDatabase.FindAssets("t:" + typeName, new[] { dirPath });
        //All 10_Flags
        // string[] allProjectFlags = AssetDatabase.FindAssets("t:GameFlagBase", new[] { "Assets/10_Flags" });
        for (var i = 0; i < allSOs.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(allSOs[i]);
            var flag = AssetDatabase.LoadAssetAtPath<T>(path);

            //  自動生成pathName

            // var pathName = path.Substring(16, path.Length - 16);
            // if (flag.flagpath != pathName)
            // {
            //     flag.flagpath = pathName;
            //     EditorUtility.SetDirty(flag);
            // }
            soList.Add(flag);
        }

        return soList;
    }
#endif
}