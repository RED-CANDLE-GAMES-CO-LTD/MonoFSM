using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MonoObj))]
public class PoolBank : AbstractDescriptionBehaviour, ISceneSavingCallbackReceiver, ISceneAwake
{
    // [InlineButton("FindOrCreatePoolPrewarmData","Create")]
    [SOConfig("15_PoolManagerPrewarm")]
    [InlineEditor]
    public PoolPrewarmData _bindPrewarmData;

    //     void FindOrCreatePoolPrewarmData()
    //     {
    //         FindPoolPrewarmDataFor(this);
    //     }
    //
    //     public static PoolPrewarmData FindPoolPrewarmDataFor(PoolBank poolbank)
    //     {
    //         if (poolbank._bindPrewarmData != null)
    //         {
    //             return poolbank._bindPrewarmData;
    //         }
    //
    //         string directoryPath = "Assets/15_PoolManagerPrewarm";
    //         var path = directoryPath + "/" + poolbank.gameObject.scene.name + "_Prewarm.asset";
    //         string resourcesId = poolbank.gameObject.scene.name + "_Prewarm";
    //
    //         PoolPrewarmData prewarmData = Resources.Load<PoolPrewarmData>(resourcesId);
    //
    //         if (prewarmData == null)
    //         {
    // #if UNITY_EDITOR
    //             prewarmData = AssetDatabase.LoadAssetAtPath<PoolPrewarmData>(path);
    // #endif
    //         }
    //         else
    //         {
    //
    //         }
    //
    //         if (prewarmData == null)
    //         {
    //             Debug.LogWarning("create new prewarm by resources!");
    // #if UNITY_EDITOR
    //             if (!AssetDatabase.IsValidFolder(directoryPath))
    //             {
    //                 AssetDatabase.CreateFolder("Assets", "15_PoolManagerPrewarm");
    //             }
    //             prewarmData = ScriptableObject.CreateInstance<PoolPrewarmData>();
    //             AssetDatabase.CreateAsset(prewarmData, path);
    //             AssetDatabase.SaveAssets();
    //             AssetDatabase.Refresh();
    // #endif
    //         }
    //
    //         poolbank._bindPrewarmData = prewarmData;
    // #if UNITY_EDITOR
    //         EditorUtility.SetDirty(poolbank);
    // #endif
    //         return prewarmData;
    //     }

    public static PoolPrewarmData FindGlobalPrewarmData()
    {
        var folderPath = "Assets/Resources/15_PoolManagerPrewarm";
        var assetName = "_Global_Prewarm.asset";
        var path = $"{folderPath}/{assetName}";
        string resourcesId = "15_PoolManagerPrewarm/_Global_Prewarm";

        // 先嘗試從 Resources 載入
        PoolPrewarmData prewarmData = Resources.Load<PoolPrewarmData>(resourcesId);

        if (prewarmData == null)
        {
#if UNITY_EDITOR
            // 若 Resources 沒有，再從 AssetDatabase 找
            prewarmData = AssetDatabase.LoadAssetAtPath<PoolPrewarmData>(path);
#endif
        }

        if (prewarmData == null)
        {
            Debug.LogWarning("create new prewarm by resources!");
#if UNITY_EDITOR
            // 資料夾不存在時建立
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parent = "Assets";
                string[] split = folderPath.Split('/');
                for (int i = 1; i < split.Length; i++)
                {
                    string currentPath = $"{parent}/{split[i]}";
                    if (!AssetDatabase.IsValidFolder(currentPath))
                    {
                        AssetDatabase.CreateFolder(parent, split[i]);
                    }
                    parent = currentPath;
                }
            }

            // 建立 ScriptableObject 資產
            prewarmData = ScriptableObject.CreateInstance<PoolPrewarmData>();
            AssetDatabase.CreateAsset(prewarmData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        return prewarmData;
    }

    public void OnBeforeSceneSave()
    {
        FindGlobalPrewarmData();
        // 確保當前 PoolBank 的 _bindPrewarmData 也被處理
        if (_bindPrewarmData == null)
        {
            // FindOrCreatePoolPrewarmData();
        }
    }

    PoolManager poolManager => _parentObj.WorldUpdateSimulator.Pool;

    public void EnterSceneAwake()
    {
        if (_bindPrewarmData == null)
            return;

        Debug.Log("EnterSceneAwake?" + this.gameObject);

        Profiler.BeginSample("Prewarm GameLevel PoolObjects");

        var pool = poolManager;
        pool.PrepareGlobalPrewarmData();
        pool.SetPrewarmData(_bindPrewarmData, this);
        pool.ReCalculatePools();
        Profiler.EndSample();

        // 顯示動態 PoolBank 機制資訊
        ShowDynamicPoolInfo();
    }

    /// <summary>
    /// 顯示動態 Pool 機制的統計資訊
    /// </summary>
    private void ShowDynamicPoolInfo()
    {
        var sceneName = gameObject.scene.name;
        Debug.Log($"=== 動態 PoolBank 機制啟動 ===");
        Debug.Log($"當前場景: {sceneName}");

        // 顯示 Protected 物件統計
        var protectedStats = GetProtectedObjectStats();
        if (protectedStats.Count > 0)
        {
            Debug.Log("Protected 狀態的物件:");
            foreach (var stat in protectedStats)
            {
                Debug.Log($"  - {stat.Key.name}: {stat.Value} 個");
            }
        }
        else
        {
            Debug.Log("目前沒有 Protected 狀態的物件");
        }

        Debug.Log("=== 使用提示 ===");
        Debug.Log("1. 使用 obj.MarkAsProtected() 保護物件不被回收");
        Debug.Log("2. 使用 obj.MarkAsReturnable() 標記物件可以回收");
        Debug.Log("3. Pool 調整時會自動保護 Protected 狀態的物件");
        Debug.Log("4. 場景配置決定 Pool 大小，Protected 物件不被強制回收");
    }

    /// <summary>
    /// 取得 Protected 物件統計
    /// </summary>
    private Dictionary<PoolObject, int> GetProtectedObjectStats()
    {
        var stats = new Dictionary<PoolObject, int>();
        var pm = this.poolManager;
        foreach (var pool in pm.allPools)
        {
            var protectedCount = 0;
            foreach (var obj in pool.OnUseObjs)
            {
                if (obj != null && obj.IsProtected())
                {
                    protectedCount++;
                }
            }

            if (protectedCount > 0)
            {
                stats[pool._prefab] = protectedCount;
            }
        }

        return stats;
    }

    /// <summary>
}
