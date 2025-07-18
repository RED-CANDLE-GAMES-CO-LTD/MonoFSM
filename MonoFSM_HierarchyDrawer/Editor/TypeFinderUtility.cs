#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TypeFinderUtility
{
    private static Dictionary<string, Type> _cachedExactName = new();
    private static List<Type> _cachedAllMonoBehaviourTypes;

    /// <summary>
    /// 透過完整名稱或簡名查找 MonoBehaviour（含自訂類別）。
    /// </summary>
    public static Type FindMonoBehaviourType(string typeName, bool allowPartialMatch = false)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        typeName = typeName.Trim().ToLower();
        // Try cached
        if (_cachedExactName.TryGetValue(typeName, out var found))
        {
            Debug.Log($"Found MonoBehaviour type from cache: {found.Name} for name: {typeName}");
            return found;
        }
            

        // Lazy load all MonoBehaviour-derived types
        _cachedAllMonoBehaviourTypes ??= TypeCache.GetTypesDerivedFrom<MonoBehaviour>().ToList();

        // First pass: exact name
        found = _cachedAllMonoBehaviourTypes.FirstOrDefault(t =>
            t.Name.ToLower() == typeName);
        if (found != null)
        {
            _cachedExactName[typeName] = found;
            Debug.Log($"Found MonoBehaviour type: {found.Name} for name: {typeName}");
            return found;
        }

        // Second pass: partial match (optional)
        if (allowPartialMatch)
        {
            found = _cachedAllMonoBehaviourTypes.FirstOrDefault(t =>
                t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                // _cachedExactName[typeName] = found;
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// 回傳所有繼承自 T 的型別（cached）。
    /// </summary>
    public static List<Type> GetAllTypesDerivedFrom<T>() where T : class
    {
        return TypeCache.GetTypesDerivedFrom<T>().ToList();
    }
}
#endif