using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

//大部分的Static Config用這個, 可以依照testMode來選擇不同組config
public class ScriptableObjectConfig<T> : ScriptableObject where T : ScriptableObject
{
    [EnumToggleButtons] public TestMode forTestMode;
}

//singleton SO, 有instance
//Singleton config，會自動載入或建立實例
public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObjectSingleton<T>
{
    // public void Validate(SelfValidationResult result) 
    //     => this.AssetInFolderValidate("Resources/Configs", result);

    private static T s_Instance;
    [PreviewInInspector] private T preview => Instance;
    
    public static T Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = LoadOrCreateInstance();
            }
            return s_Instance;
        }
    }

    private static T LoadOrCreateInstance()
    {
        // 1. 優先用 Resources.Load
        string name = typeof(T).Name;
        T inst = Resources.Load<T>(name);
        if (!inst)
        {
            inst = Resources.LoadAll<T>(string.Empty).FirstOrDefault();
        }
        if (!inst)
        {
            inst = CreateInstance<T>();
#if UNITY_EDITOR
            string dir = "Assets/Resources";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(inst, $"Assets/Resources/{name}.asset");
            AssetDatabase.SaveAssets();
#endif
        }
        return inst;
    }

    public void ManuallyAssign()
    {
        s_Instance = this as T;
    }
}