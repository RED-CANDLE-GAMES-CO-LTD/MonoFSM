using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

/// <summary>
/// 通用的 lazy instance 機制，可以被任何 ScriptableObject 使用
/// 不需要繼承特定基類，只需要呼叫靜態方法
/// </summary>
public static class LazyInstanceHelper
{
    /// <summary>
    /// 取得或建立 ScriptableObject 實例
    /// 1. 優先用類型名稱從 Resources 載入
    /// 2. 找不到時搜尋所有同類型資產
    /// 3. 還是找不到就自動建立新實例（編輯器模式）
    /// </summary>
    public static T GetOrCreateInstance<T>() where T : ScriptableObject
    {
        return GetOrCreateInstance<T>(typeof(T).Name);
    }
    
    /// <summary>
    /// 取得或建立 ScriptableObject 實例（指定資源名稱）
    /// </summary>
    public static T GetOrCreateInstance<T>(string resourceName) where T : ScriptableObject
    {
        return GetOrCreateInstance<T>(resourceName, "Assets/Resources");
    }
    
    /// <summary>
    /// 取得或建立 ScriptableObject 實例（完整參數）
    /// </summary>
    /// <param name="resourceName">資源名稱</param>
    /// <param name="createPath">建立路徑（編輯器模式）</param>
    public static T GetOrCreateInstance<T>(string resourceName, string createPath) where T : ScriptableObject
    {
        // 1. 優先用 Resources.Load
        T instance = Resources.Load<T>(resourceName);
        if (instance != null)
            return instance;
        
        // 2. 搜尋所有同類型資產
        instance = Resources.LoadAll<T>(string.Empty).FirstOrDefault();
        if (instance != null)
            return instance;
        
        // 3. 編輯器模式下自動建立
#if UNITY_EDITOR
        instance = ScriptableObject.CreateInstance<T>();
        
        if (!Directory.Exists(createPath))
            Directory.CreateDirectory(createPath);
            
        string assetPath = $"{createPath}/{resourceName}.asset";
        AssetDatabase.CreateAsset(instance, assetPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"自動建立 {typeof(T).Name} 實例：{assetPath}");
#else
        Debug.LogError($"找不到 {typeof(T).Name} 實例，且非編輯器模式無法自動建立");
#endif
        
        return instance;
    }
}

/// <summary>
/// 為 ScriptableObject 提供 lazy instance 擴展方法
/// </summary>
public static class ScriptableObjectExtensions
{
    /// <summary>
    /// 靜態 Instance 屬性的通用實作模式
    /// 使用方式：
    /// private static T _instance;
    /// public static T Instance => _instance ??= LazyInstanceHelper.GetOrCreateInstance<T>();
    /// </summary>
    public static T GetLazyInstance<T>(ref T cachedInstance) where T : ScriptableObject
    {
        return cachedInstance ??= LazyInstanceHelper.GetOrCreateInstance<T>();
    }
    
    /// <summary>
    /// 帶自訂資源名稱的版本
    /// </summary>
    public static T GetLazyInstance<T>(ref T cachedInstance, string resourceName) where T : ScriptableObject
    {
        return cachedInstance ??= LazyInstanceHelper.GetOrCreateInstance<T>(resourceName);
    }
}