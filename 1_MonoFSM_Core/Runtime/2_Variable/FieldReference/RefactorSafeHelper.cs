using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// Refactor-Safe 輔助工具，用於批量同步型別和欄位引用
    /// </summary>
    public static class RefactorSafeHelper
    {
        /// <summary>
        /// 同步 VariableTag 中的所有 MySerializedType
        /// </summary>
        public static void SyncVariableTagTypes(VariableTag variableTag)
        {
            if (variableTag == null) return;

            Debug.Log($"=== 同步 VariableTag '{variableTag.name}' 的型別引用 ===");

            var syncedCount = 0;
            var totalCount = 0;

            // 使用反射取得所有 MySerializedType 欄位
            var fields = typeof(VariableTag).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (IsMySerializedTypeField(field.FieldType))
                {
                    totalCount++;
                    var mySerializedType = field.GetValue(variableTag);
                    
                    if (mySerializedType != null)
                    {
                        var originalTypeName = GetTypeFullName(mySerializedType);
                        
                        // 呼叫 ValidateTypeReference 觸發同步
                        var validateMethod = field.FieldType.GetMethod("ValidateTypeReference");
                        if (validateMethod != null)
                        {
                            validateMethod.Invoke(mySerializedType, null);
                            
                            var newTypeName = GetTypeFullName(mySerializedType);
                            if (originalTypeName != newTypeName)
                            {
                                Debug.Log($"✓ 欄位 '{field.Name}' 型別已同步：'{originalTypeName}' -> '{newTypeName}'");
                                syncedCount++;
                            }
                        }
                    }
                }
            }

            if (syncedCount > 0)
            {
                Debug.Log($"同步完成：{syncedCount}/{totalCount} 個型別引用已更新");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(variableTag);
#endif
            }
            else
            {
                Debug.Log($"檢查完成：{totalCount} 個型別引用都已是最新的");
            }
        }

        /// <summary>
        /// 檢查 VariableTag 中的所有 MySerializedType 同步狀態
        /// </summary>
        public static void CheckVariableTagTypesSync(VariableTag variableTag)
        {
            if (variableTag == null) return;

            Debug.Log($"=== 檢查 VariableTag '{variableTag.name}' 的型別同步狀態 ===");

            var totalCount = 0;

            // 使用反射取得所有 MySerializedType 欄位
            var fields = typeof(VariableTag).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (IsMySerializedTypeField(field.FieldType))
                {
                    totalCount++;
                    var mySerializedType = field.GetValue(variableTag);
                    
                    if (mySerializedType != null)
                    {
                        Debug.Log($"--- 檢查欄位 '{field.Name}' ---");
                        
                        // 呼叫 CheckTypeNameSync
                        var checkMethod = field.FieldType.GetMethod("CheckTypeNameSync");
                        if (checkMethod != null)
                        {
                            checkMethod.Invoke(mySerializedType, null);
                        }
                    }
                }
            }

            Debug.Log($"檢查完成：已檢查 {totalCount} 個型別引用");
        }

        /// <summary>
        /// 批量同步場景中所有 VariableTag 的型別引用
        /// </summary>
        public static void SyncAllVariableTagsInScene()
        {
            Debug.Log("=== 批量同步場景中所有 VariableTag 的型別引用 ===");

            // 查找場景中所有使用 VariableTag 的組件
            var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
            var processedTags = new HashSet<VariableTag>();

            foreach (var mono in allMonoBehaviours)
            {
                var fields = mono.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(VariableTag))
                    {
                        var variableTag = field.GetValue(mono) as VariableTag;
                        if (variableTag != null && !processedTags.Contains(variableTag))
                        {
                            processedTags.Add(variableTag);
                            SyncVariableTagTypes(variableTag);
                        }
                    }
                }
            }

            Debug.Log($"批量同步完成：處理了 {processedTags.Count} 個不同的 VariableTag");
        }

        /// <summary>
        /// 批量同步專案中所有 VariableTag 資產的型別引用
        /// </summary>
        public static void SyncAllVariableTagAssets()
        {
#if UNITY_EDITOR
            Debug.Log("=== 批量同步專案中所有 VariableTag 資產的型別引用 ===");

            var guids = UnityEditor.AssetDatabase.FindAssets("t:VariableTag");
            var syncedCount = 0;

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var variableTag = UnityEditor.AssetDatabase.LoadAssetAtPath<VariableTag>(path);
                
                if (variableTag != null)
                {
                    Debug.Log($"--- 同步 VariableTag 資產: {variableTag.name} ---");
                    SyncVariableTagTypes(variableTag);
                    syncedCount++;
                }
            }

            Debug.Log($"批量同步完成：處理了 {syncedCount} 個 VariableTag 資產");
            UnityEditor.AssetDatabase.SaveAssets();
#else
            Debug.LogWarning("SyncAllVariableTagAssets 只能在編輯器中使用");
#endif
        }

        /// <summary>
        /// 檢查型別是否為 MySerializedType
        /// </summary>
        private static bool IsMySerializedTypeField(Type fieldType)
        {
            if (fieldType.IsGenericType)
            {
                var genericTypeDef = fieldType.GetGenericTypeDefinition();
                return genericTypeDef == typeof(MySerializedType<>);
            }
            return fieldType == typeof(MySerializedType);
        }

        /// <summary>
        /// 取得 MySerializedType 的 FullName
        /// </summary>
        private static string GetTypeFullName(object mySerializedType)
        {
            if (mySerializedType == null) return "";

            // 使用反射取得 _typeFullName 欄位
            var typeFullNameField = mySerializedType.GetType().GetField("_typeFullName", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (typeFullNameField != null)
            {
                return (string)typeFullNameField.GetValue(mySerializedType) ?? "";
            }

            return "";
        }

        /// <summary>
        /// 批量同步場景中所有 ChainedValueProvider 的欄位引用
        /// </summary>
        public static void SyncAllChainedValueProvidersInScene()
        {
            Debug.Log("=== 批量同步場景中所有 ChainedValueProvider 的欄位引用 ===");

            var allProviders = UnityEngine.Object.FindObjectsOfType<ChainedValueProvider>(true);
            var syncedCount = 0;

            foreach (var provider in allProviders)
            {
                Debug.Log($"--- 同步 ChainedValueProvider: {provider.name} ---");
                provider.SyncAccessChainFieldNames();
                syncedCount++;
            }

            Debug.Log($"批量同步完成：處理了 {syncedCount} 個 ChainedValueProvider");
        }

        /// <summary>
        /// 執行完整的 Refactor-Safe 同步
        /// </summary>
        public static void PerformFullSync()
        {
            Debug.Log("=== 執行完整的 Refactor-Safe 同步 ===");

            // 1. 同步所有 VariableTag 資產
            SyncAllVariableTagAssets();

            // 2. 同步場景中的 VariableTag
            SyncAllVariableTagsInScene();

            // 3. 同步場景中的 ChainedValueProvider
            SyncAllChainedValueProvidersInScene();

            Debug.Log("=== 完整 Refactor-Safe 同步完成 ===");
        }
    }
}