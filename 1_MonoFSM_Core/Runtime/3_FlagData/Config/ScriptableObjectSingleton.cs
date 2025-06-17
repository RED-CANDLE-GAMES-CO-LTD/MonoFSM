using System.ComponentModel;
using UnityEngine;
using Sirenix.OdinInspector;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;

//大部分的Static Config用這個, 可以依照testMode來選擇不同組config
public class ScriptableObjectConfig<T> : ScriptableObject where T : ScriptableObject
{
    [EnumToggleButtons] public TestMode forTestMode;
}

//singleton SO, 有instance
//Singleton config，要放到Resources的Config資料夾裡

//FIXME: 應該用另一個collection裝起來，然後事先prepare singleton, 不要用下面的路徑做法

public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
{
    // public void Validate(SelfValidationResult result) 
    //     => this.AssetInFolderValidate("Resources/Configs", result);

    private static T s_Instance;
    private static bool s_isLoaded;
    [PreviewInInspector] private T preview => Instance;
    public static T Instance
    {
        get
        {   
            if (!s_isLoaded)
            {
                //FIXME: 用Resource會悲劇, asset duplication問題，不可以reference污染
// #if UNITY_EDITOR
//                 if (Application.isPlaying == false) //GameConfig在playmode下要用AssetDatabase, 搬出去了
//                     s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(GetPath());
//                 else //其他人還在用Resources.Load
// #endif
                s_Instance = Resources.Load<T>(GetPath());
                s_isLoaded = true;
                // Debug.Log("Path:" + GetPath());
                // Debug.Log("Init" + s_Instance);
            }

            // if (Application.isPlaying == false && s_Instance == null)
            // {
            //     // Debug.LogError("ScriptableObjectSingleton: " + typeof(T).Name + " is not loaded");
            //     s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(GetPath());
            // }
            return s_Instance;
        }
    }

    public void ManuallyAssign()
    {
        s_Instance = this as T;
        s_isLoaded = true;
    }
    
    private static string GetPath() 
        => typeof(T).Name switch
        {
            //FIXME: 不該用路徑做
            "RCGCoreConfig" => "Configs/Build_RCGCoreConfig",
            "DropItemCollection" => "Configs/Drop Table Collection Config", //這個很雷改路徑就會爛掉喔
            "GameConfig" => "Configs/GameConfig",
            "MonsterGlobalConfig" => "Configs/MonsterGlobalConfig",
            "SceneTable" => "Configs/SceneTable",
            "TestModeGameFlag" => "Configs/TestModeGameFlag",
            "VRChallengeConfig"=>"Configs/VRChallengeConfig",
            "LocalizationAddressableTable" => "Configs/LocalizationAddressableTable",
            _ => "Configs/" + typeof(T).Name
        };
}