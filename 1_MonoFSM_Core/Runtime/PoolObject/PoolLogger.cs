using System;
using MonoDebugSetting;
using UnityEngine;

/// <summary>
/// 池系統統一日誌管理器 - 提供一致的錯誤處理和日誌記錄
/// </summary>
public static class PoolLogger
{
    private const string LOG_PREFIX = "[PoolSystem]";

    /// <summary>
    /// 日誌等級
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 記錄一般資訊
    /// </summary>
    public static void LogInfo(string message, UnityEngine.Object context = null)
    {
        if (RuntimeDebugSetting.IsDebugMode)
            Debug.Log($"{LOG_PREFIX} {message}", context);
    }

    /// <summary>
    /// 記錄警告
    /// </summary>
    public static void LogWarning(string message, UnityEngine.Object context = null)
    {
        Debug.LogWarning($"{LOG_PREFIX} {message}", context);
    }

    /// <summary>
    /// 記錄錯誤
    /// </summary>
    public static void LogError(string message, UnityEngine.Object context = null)
    {
        Debug.LogError($"{LOG_PREFIX} {message}", context);
    }

    /// <summary>
    /// 記錄錯誤與例外堆疊
    /// </summary>
    public static void LogError(string message, Exception exception, UnityEngine.Object context = null)
    {
        Debug.LogError($"{LOG_PREFIX} {message}: {exception.Message}\n{exception.StackTrace}", context);
    }

    /// <summary>
    /// 記錄組件相關錯誤
    /// </summary>
    public static void LogComponentError(object component, string methodName, Exception exception)
    {
        var context = component as MonoBehaviour;
        var componentName = component?.GetType().Name ?? "Unknown";
        LogError($"Error in {componentName}.{methodName}", exception, context);
    }

    /// <summary>
    /// 記錄池操作
    /// </summary>
    public static void LogPoolOperation(string operation, string poolName, UnityEngine.Object context = null)
    {
        LogInfo($"Pool Operation: {operation} on {poolName}", context);
    }

    /// <summary>
    /// 記錄池狀態
    /// </summary>
    public static void LogPoolStatus(string poolName, int totalObjects, int inUse, int available, UnityEngine.Object context = null)
    {
        LogInfo($"Pool Status [{poolName}]: Total={totalObjects}, InUse={inUse}, Available={available}", context);
    }

    /// <summary>
    /// 記錄物件保護狀態
    /// </summary>
    public static void LogProtectionStatus(string objectName, bool isProtected, UnityEngine.Object context = null)
    {
        var status = isProtected ? "Protected" : "Unprotected";
        LogInfo($"Object Protection: {objectName} is now {status}", context);
    }

    /// <summary>
    /// 記錄池回收操作
    /// </summary>
    public static void LogPoolRecycle(string poolName, int recycledCount, int protectedCount, UnityEngine.Object context = null)
    {
        LogInfo($"Pool Recycle [{poolName}]: Recycled={recycledCount}, Protected={protectedCount}", context);
    }

    /// <summary>
    /// 記錄場景生命週期事件
    /// </summary>
    public static void LogSceneLifecycle(string eventName, string objectName, UnityEngine.Object context = null)
    {
        LogInfo($"Scene Lifecycle: {eventName} on {objectName}", context);
    }

    /// <summary>
    /// 條件式日誌記錄
    /// </summary>
    public static void LogIf(bool condition, LogLevel level, string message, UnityEngine.Object context = null)
    {
        if (!condition) return;

        switch (level)
        {
            case LogLevel.Info:
                LogInfo(message, context);
                break;
            case LogLevel.Warning:
                LogWarning(message, context);
                break;
            case LogLevel.Error:
                LogError(message, context);
                break;
        }
    }

    /// <summary>
    /// 開發模式日誌 (只在 RCG_DEV 或 UNITY_EDITOR 時顯示)
    /// </summary>
    public static void LogDev(string message, UnityEngine.Object context = null)
    {
#if RCG_DEV || UNITY_EDITOR
        LogInfo($"[DEV] {message}", context);
#endif
    }

    /// <summary>
    /// 記錄異常但不中斷執行的錯誤
    /// </summary>
    public static bool TryLogError(Action action, string operationName, UnityEngine.Object context = null)
    {
        try
        {
            action?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            LogError($"Failed to {operationName}", e, context);
            return false;
        }
    }

    /// <summary>
    /// 記錄帶有返回值的操作錯誤
    /// </summary>
    public static T TryLogError<T>(Func<T> func, T defaultValue, string operationName, UnityEngine.Object context = null)
    {
        try
        {
            return func != null ? func.Invoke() : defaultValue;
        }
        catch (Exception e)
        {
            LogError($"Failed to {operationName}", e, context);
            return defaultValue;
        }
    }
}
