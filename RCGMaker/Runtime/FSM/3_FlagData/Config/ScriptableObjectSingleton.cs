using System.ComponentModel;
using UnityEngine;
using Sirenix.OdinInspector;
using RCGMaker.Core;

//大部分的Static Config用這個, 可以依照testMode來選擇不同組config
public class ScriptableObjectConfig<T> : ScriptableObjectSingleton<T> where T : ScriptableObject
{
    [EnumToggleButtons] public TestMode forTestMode;
}

//singleton SO, 有instance
//Singleton config，要放到Resources的Config資料夾裡
public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
{
    // public void Validate(SelfValidationResult result) 
    //     => this.AssetInFolderValidate("Resources/Configs", result);

    private static T s_Instance;
    private static bool s_isLoaded;
    
    public static T Instance
    {
        get
        {
            if (!s_isLoaded)
            {
                //FIXME: 用Resource會悲劇, asset duplication問題，不可以reference污染
                s_Instance = Resources.Load<T>(GetPath());
                
                s_isLoaded = true;
            }

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
            "RCGCoreConfig" => "Configs/Build_RCGCoreConfig",
            "DropItemCollection" => "Configs/Drop Collection Config",
            // "GameConfig" => "Configs/GameConfig Production",
            "MonsterGlobalConfig" => "Configs/MonsterGlobalConfig",
            "SceneTable" => "Configs/SceneTable",
            "TestModeGameFlag" => "Configs/TestModeGameFlag",
            _ => throw new InvalidEnumArgumentException()
        };
}