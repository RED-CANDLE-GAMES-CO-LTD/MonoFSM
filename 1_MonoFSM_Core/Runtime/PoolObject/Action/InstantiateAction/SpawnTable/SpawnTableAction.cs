using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.LifeCycle;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Utility;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.SpawnTable
{
    public enum SpawnPatternType
    {
        None,
        Circle,
        Grid,
        RandomSpaced
    }

    /// <summary>
    /// 使用 SpawnTableConfig 來決定生成內容的狀態機動作
    /// </summary>
    public class SpawnTableAction : AbstractStateAction
    {
        [InlineEditor] [SOConfig("Spawn Table")] [Required] [SerializeField]
        private SpawnTableConfig _spawnTable;

        [SerializeField] private Transform _spawnPosition;

        [SerializeField]
        [Tooltip("全域 override：若為 true 則整張表都用 SpawnVisual（僅視覺、不網路同步）。" +
                 "若只想混用個別項目，改在 SpawnTableEntry 逐項勾選 _isSpawningVisual")]
        public bool _isSpawningVisual;

        [BoxGroup("Scatter")]
        [EnumToggleButtons]
        [SerializeField]
        private SpawnPatternType _patternType = SpawnPatternType.None;

        [BoxGroup("Scatter")]
        [ShowIf("@_patternType == SpawnPatternType.Circle || _patternType == SpawnPatternType.RandomSpaced")]
        [SerializeField]
        private float _patternRadius = 0.5f;

        [BoxGroup("Scatter")]
        [ShowIf("_patternType", SpawnPatternType.Grid)]
        [SerializeField]
        private Vector2Int _gridSize = new Vector2Int(2, 2);

        [BoxGroup("Scatter")]
        [ShowIf("_patternType", SpawnPatternType.Grid)]
        [SerializeField]
        private float _gridSpacing = 0.3f;

        [BoxGroup("Scatter")]
        [ShowIf("_patternType", SpawnPatternType.RandomSpaced)]
        [Tooltip("點位之間的最小距離")]
        [SerializeField]
        private float _minDistance = 0.2f;

        [BoxGroup("Scatter")]
        [SerializeField]
        private bool _shufflePattern = true;

        // spawn 後的通用後處理（施力、設變數、播特效…）。
        // 例如 ScatterForceAfterSpawnProcess 取代原本寫死的 ApplyInitialForce。
        [CompRef]
        [AutoChildren]
        private IAfterSpawnProcess[] _afterSpawnActions;

        [ShowInDebugMode] [ReadOnly] private List<MonoObj> _spawnedObjects = new();

        protected override void OnActionExecuteImplement()
        {
            if (_spawnTable == null)
            {
                Debug.LogError("SpawnTableAction: SpawnTable is null", this);
                return;
            }

            if (_parentObj == null)
            {
                Debug.LogError("SpawnTableAction: No MonoObj found in parent", this);
                return;
            }

            if (_parentObj.WorldUpdateSimulator == null)
            {
                Debug.LogError("SpawnTableAction: No WorldUpdateSimulator found in _parentObj",
                    _parentObj);
                return;
            }

            var selectedEntries = _spawnTable.Resolve();
            var basePos = _spawnPosition != null ? _spawnPosition.position : transform.position;
            var rot = _spawnPosition != null ? _spawnPosition.rotation : transform.rotation;

            // 預先計算每個 entry 的生成數量（避免重複呼叫 GetSpawnCount）
            var spawnCounts = new List<int>(selectedEntries.Count);
            int totalCount = 0;
            foreach (var entry in selectedEntries)
            {
                int count = entry.GetSpawnCount();
                spawnCounts.Add(count);
                totalCount += count;
            }

            // 生成 pattern 點位
            var offsets = GeneratePatternOffsets(totalCount);
            if (_shufflePattern)
                SpawnPatternUtility.Shuffle(offsets);

            int offsetIndex = 0;
            for (int entryIndex = 0; entryIndex < selectedEntries.Count; entryIndex++)
            {
                var entry = selectedEntries[entryIndex];
                int count = spawnCounts[entryIndex];
                for (int i = 0; i < count; i++)
                {
                    // 取得 offset（如果有 pattern 的話）
                    var offset = offsetIndex < offsets.Count ? offsets[offsetIndex] : Vector3.zero;
                    var spawnPos = basePos + rot * offset; // 旋轉 offset
                    offsetIndex++;

                    // 全域 override 或該項自己標記為 visual，就走純視覺生成（不需 NetworkObject）
                    var spawnVisual = _isSpawningVisual || entry._isSpawningVisual;
                    MonoObj newObj;
                    if (spawnVisual)
                        newObj = _parentObj.WorldUpdateSimulator.SpawnVisual(entry._prefab, spawnPos, rot);
                    else
                        newObj = _parentObj.WorldUpdateSimulator.Spawn(entry._prefab, spawnPos, rot);

                    if (newObj != null)
                    {
                        newObj.gameObject.SetActive(true);
                        _spawnedObjects.Add(newObj);

                        foreach (var afterSpawnAction in _afterSpawnActions)
                            afterSpawnAction.AfterSpawn(newObj, spawnPos, rot, null);
                        // 讓 spawn 出來的物件自己的 IAfterSpawnProcess 也能處理
                        newObj.HandleAfterSpawn(spawnPos, rot, null);

                        Debug.Log($"SpawnTableAction: Spawned {entry._prefab.name}", newObj);
                    }
                }
            }
        }

        private List<Vector3> GeneratePatternOffsets(int count)
        {
            return _patternType switch
            {
                SpawnPatternType.Circle => SpawnPatternUtility.GenerateCirclePattern(count, _patternRadius),
                SpawnPatternType.Grid => SpawnPatternUtility.GenerateGridPattern(_gridSize.x, _gridSize.y, _gridSpacing),
                SpawnPatternType.RandomSpaced => SpawnPatternUtility.GenerateRandomPattern(
                    count, new Vector3(_patternRadius, 0, _patternRadius), _minDistance),
                _ => new List<Vector3>(new Vector3[count]) // None: 全部在原點
            };
        }
    }
}
