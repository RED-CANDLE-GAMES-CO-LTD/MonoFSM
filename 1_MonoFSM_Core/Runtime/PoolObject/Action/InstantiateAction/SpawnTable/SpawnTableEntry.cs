using System;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.SpawnTable
{
    /// <summary>
    /// SpawnTable 單一生成項目
    /// </summary>
    [Serializable]
    public class SpawnTableEntry
    {
        [Required]
        [AssetsOnly]
        [PreviewField(50, ObjectFieldAlignment.Left)]
        [PrefabFilter(typeof(PoolObject))]
        public MonoObj _prefab;

        [Tooltip("若為 true 則此項用 SpawnVisual（僅本地視覺、不需 NetworkObject）；" +
                 "prefab 沒有 NetworkObject 時務必勾選，否則走網路生成會噴 No NetworkObject component")]
        public bool _isSpawningVisual;

        [Min(0)] [Tooltip("權重（用於 Single/PickN 模式）")]
        public float _weight = 1f;

        [Range(0f, 1f)] [Tooltip("獨立機率（用於 Independent 模式）")]
        public float _probability = 1f;

        [MinMaxSlider(1, 10, true)] [Tooltip("生成數量範圍 (min, max)")]
        public Vector2Int _spawnCount = new Vector2Int(1, 1);

        /// <summary>
        /// 取得實際生成數量（在 min 和 max 之間隨機）
        /// </summary>
        public int GetSpawnCount()
        {
            return UnityEngine.Random.Range(_spawnCount.x, _spawnCount.y + 1);
        }
    }
}
