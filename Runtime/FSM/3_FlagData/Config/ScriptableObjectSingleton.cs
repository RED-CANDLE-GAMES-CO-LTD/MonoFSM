using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

//大部分的Static Config用這個, 可以依照testMode來選擇不同組config
public class ScriptableObjectConfig<T> : ScriptableObjectSingleton<T> where T : ScriptableObject
{
    [EnumToggleButtons] public TestMode forTestMode;
}

//singleton SO, 有instance
//Singleton config，要放到Resources的Config資料夾裡
public class ScriptableObjectSingleton<T> : ScriptableObject, ISelfValidator where T : ScriptableObject
{

    public void Validate(SelfValidationResult result)
    {
        this.AssetInFolderValidate("Resources/Configs", result);
    }
    
    private static T s_Instance;
    
    public static T Instance
    {
        get
        {
            if (s_Instance == null)
            {
// #if UNITY_EDITOR
                // var findAssets = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                // if (findAssets == null || findAssets.Length == 0)
                //     Debug.LogError($"Please create ScriptableObject typeof {typeof(T)} first...");
                // else if (findAssets.Length > 1)
                //     Debug.LogError($"ScriptableObject typeof {typeof(T)} exist multiple，please check they...");
                // else
                //     s_Instance = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(findAssets[0]));

                var assets = Resources.LoadAll<T>("Configs");
                s_Instance = assets[0];
// #else
                //TODO: 這裡要改成從Resources讀取
// if(TestModeGameFlag.Instance.test
                // s_Instance = Resources.Load<T>(typeof(T).Name);
// #endif
            }

            return s_Instance;
        }
    }
}