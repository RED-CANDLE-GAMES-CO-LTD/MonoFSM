using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// Refactor-Safe 管理器，提供全域的同步和檢查功能
    /// </summary>
    [CreateAssetMenu(menuName = "RCG/Refactor-Safe Manager", fileName = "RefactorSafeManager")]
    public class RefactorSafeManager : ScriptableObject
    {
        // [Header("全域 Refactor-Safe 管理工具")]
        [InfoBox("此工具可以批量同步專案中所有的型別和欄位引用，確保 refactor 後的一致性。")]

        // [Space(10)]
        [Title("專案資產同步")]
        /// <summary>
        /// 同步專案中所有 VariableTag 資產的型別引用
        /// </summary>
        [Button("同步所有 VariableTag 資產", ButtonSizes.Large)]
        [PropertyTooltip("掃描並同步專案中所有 VariableTag 資產的型別引用")]
        public void SyncAllVariableTagAssets()
        {
            RefactorSafeHelper.SyncAllVariableTagAssets();
        }

        /// <summary>
        /// 同步場景中所有 VariableTag 的型別引用
        /// </summary>
        [Button("同步場景中所有 VariableTag", ButtonSizes.Large)]
        [PropertyTooltip("同步當前場景中所有使用的 VariableTag 型別引用")]
        public void SyncAllVariableTagsInScene()
        {
            RefactorSafeHelper.SyncAllVariableTagsInScene();
        }

        // [Space(10)]
        [Title("欄位引用同步")]
        /// <summary>
        /// 同步場景中所有 ChainedValueProvider 的欄位引用
        /// </summary>
        [Button("同步場景中所有 ChainedValueProvider", ButtonSizes.Large)]
        [PropertyTooltip("同步當前場景中所有 ChainedValueProvider 的欄位引用")]
        public void SyncAllChainedValueProvidersInScene()
        {
            RefactorSafeHelper.SyncAllChainedValueProvidersInScene();
        }

        // [Space(10)]
        [Title("完整同步")]
        /// <summary>
        /// 執行完整的 Refactor-Safe 同步
        /// </summary>
        [Button("執行完整同步", ButtonSizes.Large)]
        [PropertyTooltip("執行所有類型的 Refactor-Safe 同步：VariableTag 資產、場景中的 VariableTag、以及 ChainedValueProvider")]
        [GUIColor(0.4f, 1f, 0.4f)]
        public void PerformFullSync()
        {
            RefactorSafeHelper.PerformFullSync();
        }

        // [Space(10)]
        [Title("批量驗證")]
        /// <summary>
        /// 驗證專案中所有 FieldReference 資產
        /// </summary>
        [Button("驗證所有 FieldReference 資產", ButtonSizes.Medium)]
        [PropertyTooltip("驗證專案中所有 FieldReference 資產的有效性")]
        public void ValidateAllFieldReferenceAssets()
        {
#if UNITY_EDITOR
            Debug.Log("=== 驗證所有 FieldReference 資產 ===");

            var guids = UnityEditor.AssetDatabase.FindAssets("t:FieldReference");
            var validCount = 0;
            var invalidCount = 0;

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var fieldRef = UnityEditor.AssetDatabase.LoadAssetAtPath<FieldReference>(path);

                if (fieldRef != null)
                {
                    var isValid = fieldRef.ValidateReference();
                    if (isValid)
                        validCount++;
                    else
                        invalidCount++;
                }
            }

            Debug.Log($"驗證完成：{validCount} 有效，{invalidCount} 無效，總計 {validCount + invalidCount} 個 FieldReference 資產");
#else
            Debug.LogWarning("此功能只能在編輯器中使用");
#endif
        }

        /// <summary>
        /// 驗證專案中所有 ValueAccessChain 資產
        /// </summary>
        [Button("驗證所有 ValueAccessChain 資產", ButtonSizes.Medium)]
        [PropertyTooltip("驗證專案中所有 ValueAccessChain 資產的有效性")]
        public void ValidateAllValueAccessChainAssets()
        {
#if UNITY_EDITOR
            Debug.Log("=== 驗證所有 ValueAccessChain 資產 ===");

            var guids = UnityEditor.AssetDatabase.FindAssets("t:ValueAccessChain");
            var validCount = 0;
            var invalidCount = 0;

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var accessChain = UnityEditor.AssetDatabase.LoadAssetAtPath<ValueAccessChain>(path);

                if (accessChain != null)
                {
                    var isValid = accessChain.ValidateChain();
                    if (isValid)
                        validCount++;
                    else
                        invalidCount++;
                }
            }

            Debug.Log($"驗證完成：{validCount} 有效，{invalidCount} 無效，總計 {validCount + invalidCount} 個 ValueAccessChain 資產");
#else
            Debug.LogWarning("此功能只能在編輯器中使用");
#endif
        }

        // [Space(10)]
        [Title("統計資訊")]
        /// <summary>
        /// 顯示專案統計資訊
        /// </summary>
        [Button("顯示專案統計", ButtonSizes.Medium)]
        [PropertyTooltip("顯示專案中 Refactor-Safe 相關資產的統計資訊")]
        public void ShowProjectStatistics()
        {
#if UNITY_EDITOR
            Debug.Log("=== Refactor-Safe 專案統計 ===");

            var variableTagCount = UnityEditor.AssetDatabase.FindAssets("t:VariableTag").Length;
            var fieldReferenceCount = UnityEditor.AssetDatabase.FindAssets("t:FieldReference").Length;
            var valueAccessChainCount = UnityEditor.AssetDatabase.FindAssets("t:ValueAccessChain").Length;

            // 統計場景中的組件
            var chainedValueProviders = FindObjectsOfType<ChainedValueProvider>(true);
            var activeChainedProviders = chainedValueProviders.Count(p => p.IsChainValid);

            Debug.Log($"VariableTag 資產: {variableTagCount}");
            Debug.Log($"FieldReference 資產: {fieldReferenceCount}");
            Debug.Log($"ValueAccessChain 資產: {valueAccessChainCount}");
            Debug.Log($"場景中的 ChainedValueProvider: {chainedValueProviders.Length} (有效: {activeChainedProviders})");
            Debug.Log($"總計: {variableTagCount + fieldReferenceCount + valueAccessChainCount} 個 Refactor-Safe 資產");
#else
            Debug.LogWarning("此功能只能在編輯器中使用");
#endif
        }

        [Space(10)]
        [Title("使用指南")]
        [InfoBox(@"使用建議：

1. 日常開發：系統會自動同步大部分變更，通常無需手動操作

2. 重大 Refactor 後：
   - 點擊 '執行完整同步' 確保所有引用都是最新的
   - 使用驗證功能檢查是否有問題

3. 定期維護：
   - 使用 '顯示專案統計' 了解專案規模
   - 定期執行驗證確保系統健康

4. 問題排查：
   - 如果遇到引用問題，先嘗試相應的同步功能
   - 查看 Console 輸出了解詳細資訊", InfoMessageType.Info)]
        [Space(5)]
        [ReadOnly]
        [DisplayAsString]
        public string version = "1.0.0";
    }
}