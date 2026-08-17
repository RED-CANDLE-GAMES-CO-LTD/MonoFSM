using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MonoFSM.Variable;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.Editor.Utility
{
    /// <summary>
    /// 掃過專案裡所有 MySerializedType，把靠 fallback 才解析得出來的舊型別名稱寫回資產。
    /// 改過 namespace / 搬過 assembly 之後跑一次，就不用每次 domain reload 都重新 fallback
    /// </summary>
    public static class MySerializedTypeMigrationTool
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        //序列化資料不會無限深，設個上限避免遇到意外的循環結構時卡死
        private const int MaxDepth = 12;

        [MenuItem("Tools/MonoFSM/Refactor Safe/重新解析並修復所有 MySerializedType")]
        public static void MigrateAll()
        {
            Run(true);
        }

        [MenuItem("Tools/MonoFSM/Refactor Safe/檢查 MySerializedType（不修改）")]
        public static void CheckAll()
        {
            Run(false);
        }

        private static void Run(bool applyFix)
        {
            var scanned = 0;
            var fixedCount = 0;
            var brokenReport = new StringBuilder();
            var brokenCount = 0;

            var guids = new List<string>();
            guids.AddRange(AssetDatabase.FindAssets("t:ScriptableObject"));
            guids.AddRange(AssetDatabase.FindAssets("t:Prefab"));

            try
            {
                for (var i = 0; i < guids.Count; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (
                        EditorUtility.DisplayCancelableProgressBar(
                            applyFix ? "修復 MySerializedType" : "檢查 MySerializedType",
                            $"({i + 1}/{guids.Count}) {path}",
                            (float)i / guids.Count
                        )
                    )
                        break;

                    foreach (var owner in LoadOwners(path))
                    {
                        if (owner == null)
                            continue;

                        var holders = new List<IMySerializedType>();
                        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
                        Collect(owner, owner, holders, visited, 0);

                        foreach (var holder in holders)
                        {
                            scanned++;
                            holder.BindObject = owner;

                            var before = holder.SerializedTypeName;
                            if (string.IsNullOrEmpty(before))
                                continue;

                            //ValidateTypeReference 解析成功時會自己把名稱寫回並 SetDirty
                            var resolved = holder.ValidateTypeReference();

                            if (!resolved)
                            {
                                brokenCount++;
                                brokenReport.AppendLine($"  {path} → {before}");
                                continue;
                            }

                            if (holder.SerializedTypeName != before)
                            {
                                fixedCount++;
                                if (applyFix)
                                    EditorUtility.SetDirty(owner);
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (applyFix && fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[MySerializedType] 已修復並存檔 {fixedCount} 筆過時的型別名稱");
            }
            else if (fixedCount > 0)
            {
                Debug.Log($"[MySerializedType] 有 {fixedCount} 筆名稱過時（未修改，執行「重新解析並修復」才會寫回）");
            }

            if (brokenCount > 0)
                Debug.LogWarning(
                    $"[MySerializedType] {brokenCount} 筆完全解析不到，需要手動重選型別、或在型別上補 [FormerlyFullName]：\n{brokenReport}"
                );

            Debug.Log(
                $"[MySerializedType] 掃描完成：{guids.Count} 個資產、{scanned} 筆型別引用，過時 {fixedCount}、失效 {brokenCount}"
            );
        }

        private static IEnumerable<Object> LoadOwners(string path)
        {
            //prefab 要連子物件上的所有 component 一起看，ScriptableObject 則可能有 sub asset
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset == null)
                    continue;

                if (asset is GameObject go)
                {
                    foreach (var comp in go.GetComponentsInChildren<Component>(true))
                        if (comp != null)
                            yield return comp;
                }
                else
                {
                    yield return asset;
                }
            }
        }

        /// <summary>
        /// 沿著序列化欄位往下走，收集所有 IMySerializedType。
        /// 不跨 UnityEngine.Object 引用，那是另一個資產的事，會由它自己那輪掃到
        /// </summary>
        private static void Collect(
            object node,
            Object root,
            List<IMySerializedType> results,
            HashSet<object> visited,
            int depth
        )
        {
            if (node == null || depth > MaxDepth)
                return;

            if (node is IMySerializedType holder)
            {
                if (visited.Add(holder))
                    results.Add(holder);
                return;
            }

            //只有起點那個 Object 要往內走，其他 Object 欄位都是外部引用
            if (node is Object && !ReferenceEquals(node, root))
                return;

            if (!visited.Add(node))
                return;

            if (node is IList list)
            {
                foreach (var item in list)
                    Collect(item, root, results, visited, depth + 1);
                return;
            }

            var type = node.GetType();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var field in t.GetFields(FieldFlags))
                {
                    if (!IsSerializedField(field))
                        continue;

                    if (!MayContainSerializedType(field.FieldType))
                        continue;

                    object value;
                    try
                    {
                        value = field.GetValue(node);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    Collect(value, root, results, visited, depth + 1);
                }
            }
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsNotSerialized)
                return false;
            if (field.IsDefined(typeof(NonSerializedAttribute), false))
                return false;
            return field.IsPublic || field.IsDefined(typeof(SerializeField), false);
        }

        /// <summary>
        /// 先擋掉不可能藏著 MySerializedType 的欄位，避免對每個 string/float 都做一次遞迴
        /// </summary>
        private static bool MayContainSerializedType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
                return false;
            if (type == typeof(string) || type == typeof(decimal))
                return false;
            //Unity 內建的 struct（Vector3、Color…）不會包 MySerializedType
            if (type.IsValueType && type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
                return false;
            return true;
        }

        private class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
