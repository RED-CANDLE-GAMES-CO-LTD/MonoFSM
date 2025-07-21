using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Text;
using MonoFSM.Core;
using MonoDebugSetting;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;


public static class MonoExtensionLogger
{
#if UNITY_EDITOR
    [InitializeOnEnterPlayMode]
    private static void OnEnterPlayModeInEditor(EnterPlayModeOptions options)
    {
        // Debug.Log("Entering PlayMode");
        DebugProviderDict.Clear();
    }
#endif

    //static DebugProvider in parent dictionary
    private static readonly Dictionary<Component, DebugProvider[]> DebugProviderDict = new();

    public static (bool, DebugProvider) IsLoggingCheck(Component comp)
    {
// #if RCG_DEV

        if (DebugSetting.IsDebugMode == false)
            return (false, null);
        // var isLogging = false;
        // var providerName = "";

        //FIXME: 陣列有gc...
        if (comp == null)
        {
            // Debug.LogError("Component is null!?");
            return (false, null);
        }

        var hasProvider = DebugProviderDict.TryGetValue(comp, out var providers);


        //空的
        if (!hasProvider)
        {
            providers = comp.GetComponentsInParent<DebugProvider>(true);
            DebugProviderDict.Add(comp, providers);
        }

        var refetchNeeded = false;

        //FIXME:這個有memory leak, debug mode會錯唷
        foreach (var item in providers)
        {
            if (item == null)
            {
                Debug.LogError("DebugProvider is null", comp);
                DebugProviderDict.Remove(comp);
                refetchNeeded = true;
                break;
            }
        }

        if (refetchNeeded)
        {
            providers = comp.GetComponentsInParent<DebugProvider>(true);
            DebugProviderDict.Add(comp, providers);
        }


        foreach (var item in providers)
            if (item.IsLogInChildren)
            {
                // providerName = item.gameObject.name;
                return (true, item);
            }

        return (false, null);
    }

    // public static void LogContext(this Component go, string s1)
    // {
    //     var (isLogging, provider) = IsLoggingCheck(go);
    //
    //
    //     if (isLogging)
    //     {
    //         var result = ZString.Concat("[", provider, provider.GetInstanceID(), ",", go.GetInstanceID(), "]\n",
    //             ZString.Join(",", s1));
    //         FinalLog(go, result, provider);
    //     }
    // }

    [HideInCallstack]
    [Conditional("UNITY_EDITOR")]
    public static void Log(this Component go, string s1)
    {
        var (isLogging, provider) = IsLoggingCheck(go);

        if (isLogging)
        {
            var result = ZString.Concat("[",provider.GetInstanceID(), "]\n",
                ZString.Join(",", s1));
            FinalLog(go, result, provider);
        }
    }

    public static string ColorTag(this string str, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
    }

    public static string ColorTag<T>(this T strObj, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{strObj}</color>";
    }


    [Conditional("UNITY_EDITOR")]
    public static void Log<T1, T2>(this Component go, T1 s1, T2 s2)
    {
#if RCG_DEV
        var (isLogging, provider) = IsLoggingCheck(go);

        if (isLogging)
        {
            // var fullStr = string.Join(",", items);
            var result = ZString.Concat("[",provider.GetInstanceID(), "]", s1, " ", s2);
            FinalLog(go, result, provider);
        }
#endif
    }

    [HideInCallstack]
    [Conditional("UNITY_EDITOR")]
    public static void Log<T1, T2, T3>(this Component go, T1 s1, T2 s2, T3 s3)
    {
#if RCG_DEV
        var (isLogging, provider) = IsLoggingCheck(go);

        if (isLogging)
        {
            var result = ZString.Concat("[" ,provider.GetInstanceID(), "]", s1, " ", s2, " ", s3);
            FinalLog(go, result, provider);
        }
#endif
    }


    [HideInCallstack]
    [Conditional("UNITY_EDITOR")]
    public static void Log<T1, T2, T3, T4>(this Component go, T1 s1, T2 s2, T3 s3, T4 s4)
    {
#if RCG_DEV

        var (isLogging, providerName) = IsLoggingCheck(go);

        if (isLogging)
        {
            // var fullStr = string.Join(",", items);
            var result = ZString.Concat("[", providerName, go.GetInstanceID(), "]\n", s1, " ", s2, " ", s3, " ",
                s4);
            FinalLog(go, result, providerName);
        }
#endif
    }

    [HideInCallstack]
    [Conditional("UNITY_EDITOR")]
    public static void Log<T1, T2, T3, T4, T5>(this Component go, T1 s1, T2 s2, T3 s3, T4 s4, T5 s5)
    {
#if RCG_DEV

        var (isLogging, providerName) = IsLoggingCheck(go);
        if (!isLogging) return;
        var result = ZString.Concat("[", providerName, go.GetInstanceID(), "]\n", s1, " ", s2, " ", s3, " ", s4,
            " ", s5);
        FinalLog(go, result, providerName);
#endif
    }

    [HideInCallstack]
    [Conditional("UNITY_EDITOR")]
    public static void Log<T1, T2, T3, T4, T5, T6>(this Component go, T1 s1, T2 s2, T3 s3, T4 s4, T5 s5, T6 s6)
    {
#if RCG_DEV

        var (isLogging, providerName) = IsLoggingCheck(go);
        if (!isLogging) return;
        var result = ZString.Concat("[", providerName, go.GetInstanceID(), "]\n", s1, " ", s2, " ", s3, " ", s4,
            " ", s5, " ", s6);
        FinalLog(go, result, providerName);
#endif
    }

    //這個還是舊規
    [Conditional("UNITY_EDITOR")]
    public static void
        LogWarning(this Component go, object message, UnityEngine.Object context = null) //where T : Component
    {
#if RCG_DEV
        var (isLogging, provider) = IsLoggingCheck(go);

        if (isLogging)
        {
            // var fullStr = string.Join(",", items);
            var result = ZString.Concat("[", provider, go.GetInstanceID(), "]\n", message);
            FinalLog(go, result, provider, LogType.Warning);
        }
#endif
    }

    [HideInCallstack]
    [Conditional("UNITY_EDITOR")]
    public static void
        LogError<T1>(this Component go, T1 s1) //where T : Component
    {
#if RCG_DEV
        var (isLogging, provider) = IsLoggingCheck(go);

        if (isLogging)
        {
            // var fullStr = string.Join(",", items);
            var result = ZString.Concat("[", provider, go.GetInstanceID(), "]\n", s1);
            FinalLog(go, result, provider, LogType.Error);
        }
#endif
    }

    [HideInCallstack]
    [Conditional("UNITY_EDITOR")]
    public static void
        LogError<T1, T2>(this Component go, T1 s1, T2 s2) //where T : Component
    {
#if UNITY_EDITOR
        var (isLogging, provider) = IsLoggingCheck(go);
        if (isLogging)
        {
            // var fullStr = string.Join(",", items);
            var result = ZString.Concat("[", provider, go.GetInstanceID(), "]\n", s1, " ", s2);
            FinalLog(go, result, provider, LogType.Error);
        }
#endif
    }

    #region LogError T1~T3

    [HideInCallstack]
    [Conditional("RCG_DEV")]
    public static void
        LogError<T1, T2, T3>(this Component go, T1 s1, T2 s2, T3 s3) //where T : Component
    {
#if RCG_DEV
        var (isLogging, provider) = IsLoggingCheck(go);
        if (isLogging)
        {
            // var fullStr = string.Join(",", items);
            var result = ZString.Concat("[", provider, go.GetInstanceID(), "]\n", s1, " ", s2, " ", s3);
            FinalLog(go, result, provider, LogType.Error);
        }
#endif
    }

    #endregion

    #region LogError T1~T4

    [HideInCallstack]
    [Conditional("RCG_DEV")]
    public static void
        LogError<T1, T2, T3, T4>(this Component go, T1 s1, T2 s2, T3 s3, T4 s4) //where T : Component
    {
#if RCG_DEV
        var (isLogging, provider) = IsLoggingCheck(go);
        if (isLogging)
        {
            // var fullStr = string.Join(",", items);
            var result = ZString.Concat("[", provider, go.GetInstanceID(), "]\n", s1, " ", s2, " ", s3, " ", s4);
            FinalLog(go, result, provider, LogType.Error);
        }
#endif
    }

    #endregion


    [HideInCallstack]
    private static void FinalLog(Component go, string message, DebugProvider provider, LogType type = LogType.Log)
    {
        // provider.SaveLog(message, go);
        switch (type)
        {
            case LogType.Log:
                // if (provider.currentState)
                // {
                //     //多餘？
                //     message = ZString.Concat(message,"\n at state:", provider.currentState, ",frame:",
                //         provider.currentState.CurrentFrameCount);
                // }

                Debug.Log(message, go);
                break;
            case LogType.Error:
                Debug.LogError(message, go);
                break;
            case LogType.Warning:
                Debug.LogWarning(message, go);
                break;
        }

        if (go.TryGetComponent<RCGBreakPoint>(out var breakPoint))
        {
            if (breakPoint.isOn)
            {
                Debug.Log("Break By BreakPoint", go);
                Debug.Break();
            }
        }
    }

    // [Conditional("UNITY_EDITOR")]
    // public static void Log(this Component go, object message) //where T : Component
    // {
    //     Log(go, go.gameObject, message);
    // }
}