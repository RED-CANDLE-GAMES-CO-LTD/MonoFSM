using _1_MonoFSM_Core.Runtime.Utilities;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using MonoFSM.Core.Variable;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// 一次 spawn 隨機 N 顆同一種 prefab（數量 = [_minCount, _maxCount]，兩端都含），每顆給「向上 + 隨機水平方向」的初速度散開。
    /// 用在開寶箱噴金幣這類「一把撒出去」的場合；單顆 spawn 用 SpawnAction 就好。
    /// 數量、位置抖動、水平方向都走 TickRandom（seed + tick），聯網 deterministic；要 per-instance 各異就掛一個 IIntProvider 子物件。
    /// 初速度用 ForceMode.VelocityChange，跟 prefab 的質量無關。子節點的 IAfterSpawnProcess 也會對每一顆跑。
    /// 網路物件只該在 State Authority 跑（掛在只跑 SA 的 event 底下，例如 OnStateEnter）。迴圈內不配置記憶體。
    /// </summary>
    public class SpawnBurstAction : AbstractStateAction, IPoolObjectPlayer
    {
        /// <summary>最後一次執行的結果；每個 early return 前都會先寫這顆。</summary>
        public enum BurstState
        {
            /// <summary>還沒執行過</summary>
            NotExecutedYet,

            /// <summary>正常噴完</summary>
            Spawned,

            /// <summary>prefab 沒指定</summary>
            NoPrefab,

            /// <summary>找不到 parent MonoObj</summary>
            NoParentObj,

            /// <summary>parent MonoObj 沒有 WorldUpdateSimulator</summary>
            NoSimulator,

            /// <summary>算出來的數量 &lt;= 0</summary>
            ZeroCount,
        }

        [InlineField]
        [SerializeField]
        private VarMonoObjFoldOut _poolObjFoldOut;

        [Tooltip("生成中心點；沒填就用這個 action 節點的位置")]
        [SerializeField]
        private Transform _spawnPosition;

        [BoxGroup("數量")]
        [Tooltip("最少噴幾顆（含）")]
        [DropDownRef]
        [SerializeField]
        private VarInt _minCount;

        [BoxGroup("數量")]
        [Tooltip("最多噴幾顆（含）")]
        [DropDownRef]
        [SerializeField]
        private VarInt _maxCount;

        [BoxGroup("數量")]
        [Tooltip("_minCount / _maxCount 沒接 Var 時用的常數")]
        [SerializeField]
        private Vector2Int _fallbackCountRange = new(3, 6);

        [BoxGroup("初速度")]
        [Tooltip("向上速度範圍 (m/s)")]
        [SerializeField]
        private Vector2 _upSpeedRange = new(3.5f, 5f);

        [BoxGroup("初速度")]
        [Tooltip("水平速度範圍 (m/s)，方向隨機")]
        [SerializeField]
        private Vector2 _horizontalSpeedRange = new(1f, 2.5f);

        [BoxGroup("初速度")]
        [Tooltip("生成位置在水平面上的隨機抖動半徑 (m)，避免一堆物件生在同一點互相炸開")]
        [SerializeField]
        private float _positionJitterRadius = 0.12f;

        [BoxGroup("初速度")]
        [Tooltip("隨機角速度上限 (rad/s)，0 = 不轉")]
        [SerializeField]
        private float _maxAngularSpeed = 8f;

        [Tooltip("固定 salt，聯網時每台機器要一致")]
        [SerializeField]
        private int _seed = 24680;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private IIntProvider _seedSource;

        [CompRef]
        [AutoChildren]
        private IAfterSpawnProcess[] _afterSpawnActions;

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private BurstState _lastState = BurstState.NotExecutedYet;

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private int _lastSpawnCount;

        private int Seed => _seedSource != null ? TickRandom.Combine(_seed, _seedSource.IntValue) : _seed;

        public override string Description => "Spawn Burst " + _poolObjFoldOut?.Description;

        protected override void OnActionExecuteImplement()
        {
            var prefab = _poolObjFoldOut?.Value;
            if (prefab == null)
            {
                _lastState = BurstState.NoPrefab;
                Debug.LogError("SpawnBurstAction: prefab is null", this);
                return;
            }

            if (_parentObj == null)
            {
                _lastState = BurstState.NoParentObj;
                Debug.LogError("SpawnBurstAction: no parent MonoObj", this);
                return;
            }

            var simulator = _parentObj.WorldUpdateSimulator;
            if (simulator == null)
            {
                _lastState = BurstState.NoSimulator;
                Debug.LogError("SpawnBurstAction: parent MonoObj has no WorldUpdateSimulator", this);
                return;
            }

            var min = _minCount != null ? _minCount.CurrentValue : _fallbackCountRange.x;
            var max = _maxCount != null ? _maxCount.CurrentValue : _fallbackCountRange.y;
            if (max < min)
                max = min;

            var seed = Seed;
            var tick = WorldUpdateSimulator.CurrentTick;
            var count = TickRandom.RangeInt(seed, tick, min, max + 1);
            _lastSpawnCount = count;
            if (count <= 0)
            {
                _lastState = BurstState.ZeroCount;
                Debug.LogWarning("SpawnBurstAction: count <= 0, skip", this);
                return;
            }

            var center = _spawnPosition != null ? _spawnPosition.position : transform.position;
            var rotation = _spawnPosition != null ? _spawnPosition.rotation : transform.rotation;

            for (var i = 0; i < count; i++)
            {
                // 每顆、每個用途各一條 salt，避免同 tick 的值互相相關
                var s = TickRandom.Combine(seed, i * 7 + 1);
                var angle = TickRandom.Range(s, tick, 0f, Mathf.PI * 2f);
                var dirX = Mathf.Cos(angle);
                var dirZ = Mathf.Sin(angle);
                var jitter = TickRandom.Range(TickRandom.Combine(s, 2), tick, 0f, _positionJitterRadius);
                var pos = center + new Vector3(dirX * jitter, 0f, dirZ * jitter);

                var newObj = simulator.Spawn(prefab, pos, rotation, this);
                if (newObj == null)
                    continue;
                newObj.gameObject.SetActive(true);

                var rb = newObj.GetCompCache<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    var up = TickRandom.Range(TickRandom.Combine(s, 3), tick, _upSpeedRange.x, _upSpeedRange.y);
                    var hSpeed = TickRandom.Range(TickRandom.Combine(s, 4), tick, _horizontalSpeedRange.x,
                        _horizontalSpeedRange.y);
                    rb.AddForce(new Vector3(dirX * hSpeed, up, dirZ * hSpeed), ForceMode.VelocityChange);

                    if (_maxAngularSpeed > 0f)
                    {
                        var av = new Vector3(
                            TickRandom.Range(TickRandom.Combine(s, 5), tick, -1f, 1f),
                            TickRandom.Range(TickRandom.Combine(s, 6), tick, -1f, 1f),
                            TickRandom.Range(TickRandom.Combine(s, 7), tick, -1f, 1f)) * _maxAngularSpeed;
                        rb.AddTorque(av, ForceMode.VelocityChange);
                    }
                }

                foreach (var process in _afterSpawnActions)
                    process.AfterSpawn(newObj, pos, rotation, null);

                newObj.HandleAfterSpawn(pos, rotation, null);
            }

            _lastState = BurstState.Spawned;
        }
    }
}
