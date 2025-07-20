using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EditorComponentHelper
{
    /// <summary>
    /// 在 Editor 時期自動取得 Component，Play 時如果為 null 則顯示錯誤
    /// </summary>
    /// <typeparam name="T">要取得的 Component 類型</typeparam>
    /// <param name="target">目標 GameObject 或 Component</param>
    /// <param name="component">要檢查/設定的 component 參考</param>
    /// <param name="logIfMissing"></param>
    /// <returns>是否成功取得 Component</returns>
    public static bool EnsureComponent<T>(this MonoBehaviour target, ref T component, bool logIfMissing = true)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && component == null)
            component = target.GetComponent<T>();
#endif

        EnsureComponentLog(target, ref component, logIfMissing);

        return component != null;
    }

    private static bool EnsureComponentLog<T>(MonoBehaviour target, ref T component, bool logIfMissing)
    {
        if (Application.isPlaying && component == null && logIfMissing)
        {
            var message =
                $"{typeof(T).Name} is null, please ensure it is assigned in the inspector or added as component.";
            Debug.LogError(message, target);
            return false;
        }

        return component != null;
    }


    public static void EnsureComponentInChildren<T>(this MonoBehaviour target, ref T component, bool required = true)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && component == null)
            component = target.GetComponentInChildren<T>();
#endif
        EnsureComponentLog(target, ref component, required);
    }

    /// <summary>
    /// 在 Editor 時期自動取得 Component（從父物件尋找），Play 時如果為 null 則顯示錯誤
    /// </summary>
    public static void EnsureComponentInParent<T>(this MonoBehaviour target, ref T component, bool logIfMissing = true)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && component == null) component = target.GetComponentInParent<T>();
#endif

        EnsureComponentLog(target, ref component, logIfMissing);
    }
}