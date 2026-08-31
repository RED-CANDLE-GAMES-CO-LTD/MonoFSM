using System.Collections.Generic;
using MonoDebugSetting;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Detection;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using MonoFSM_Physics.Runtime.Interact.SpatialDetection;
using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    /// <summary>
    ///     Cast 偵測器的基礎類別，在 SimulateUpdate 時進行物理檢測並快取結果。
    ///     子類只需實作 PerformCast 即可。
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class AbstractCastCache
        : AbstractDescriptionBehaviour,
            IBeforeSimulate,
            IUpdateSimulate,
            IResetStateRestore,
            IHierarchyValueInfo,
            IRenderUpdate,
            ISceneAwake
    {
        public int SimulateOrder => -1;

        //cast 必須早於同一 MonoObj 子樹裡的 EffectDetector（也在 BeforeSimulate），
        //不然 CastDetectSource 讀到的是上一 tick 的 CachedHits
        public int BeforeSimulateOrder => -100;

        //FIXME: 這要幹嘛？
        [SerializeField]
        protected Transform _cacheOrigin;

        //FIXME: 這要幹嘛？
        [SerializeField]
        protected Transform _cacheEndPoint;

        // protected bool IsHavingDistanceSource => _distanceSource != null;

        // [HideIf(nameof(IsHavingDistanceSource))]
        public VarFloatWrapper _distance = new VarFloatWrapper(20);

        // [FormerlySerializedAs("_distanceProvider")]
        // [CompRef]
        // [Auto]
        // [SerializeField]
        // private DistanceSourceFromSpeed _distanceSource;

        [ShowInInspector]
        public float GetDistance()
        {
            // if (_distanceSource != null)
            //     return _distanceSource.Distance;
            return _distance.Value;
        }

        [FormerlySerializedAs("HittingLayer")]
        // [CustomSerializable]
        public LayerMask _hittingLayer;

        [PreviewInInspector]
        private Collider firstHitCollider =>
            CachedHits is { Count: > 0 } ? CachedHits[0].collider : null;

        // 預分配的 list，每幀 Clear + Add 避免 GC
        [ShowInInspector]
        [ReadOnly]
        [ListDrawerSettings(IsReadOnly = true)]
        private readonly List<Collider> _hitColliders = new();

        private DualPhaseValue<List<RaycastHit>> _cachedHits = new();

        [PreviewInInspector] public List<RaycastHit> CachedHits => _cachedHits.Value;
        public RaycastHit CachedHit => CachedHits is { Count: > 0 } ? CachedHits[0] : default;

        [Tooltip("set 結果用")]
        public VarBool _hasHitVar;

        [Tooltip("set 結果用")]
        public VarVector3 _hitPosVar;

        [Tooltip("set 結果用")]
        public VarTransform _hitPosVarTransform;

        [ShowInInspector] public Vector3 rayOri => CachedRay.origin;
        [ShowInInspector] public Vector3 rayDir => CachedRay.direction;

        [ShowInInspector]
        public Ray CachedRay => _cachedRay.Value;
        [ShowInInspector] private DualPhaseValue<Ray> _cachedRay = new();

#if UNITY_EDITOR
        [ShowInDebugMode]
        private readonly Queue<Collider> _debugHistoryObjs = new();
#endif

        public bool _isDrawDebugColor;

        [SerializeField]
        protected Color _overrideGizmoColor = Color.red;

        public QueryTriggerInteraction _queryTriggerInteraction = QueryTriggerInteraction.Collide;

        public bool _singleHitOnly;

        [Required]
        [Auto]
        [CompRef]
        protected AbstractRayProvider _rayProvider;

        public bool _manualUpdateMode;

        [Title("忽略命中")]
        [SerializeField]
        private IgnoreColliderFilter _ignoreFilter = new();

        public IgnoreColliderFilter IgnoreFilter => _ignoreFilter;

#if UNITY_EDITOR
        [ShowInDebugMode]
        [ReadOnly]
        private int _debugIgnoredHitCount;
#endif

        protected readonly RaycastHit[] _castResultsBuffer = new RaycastHit[20];

        Vector3 _prevHitPos;
        Vector3 _currHitPos;

        // --- Lifecycle ---

        public void EnterSceneAwake()
        {
            _cachedHits.SimValue = new List<RaycastHit>();
            _cachedHits.RenderValue = new List<RaycastHit>();

            _ignoreFilter.Init(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _ignoreFilter.EditorRefreshPreview(this);
        }
#endif

        public void BeforeSimulate(float deltaTime)
        {
            TryCast();
        }

        //client 怎麼做？ proxy? 直接同步point?
        public void Simulate(float deltaTime)
        {

        }

        public void Render(float deltaTime)
        {
        }

        public void ResetStateRestore(bool IsHardReset)
        {
            _cachedRay.Reset();
            _cachedHits.SimValue?.Clear();
            _cachedHits.RenderValue?.Clear();
            _hitColliders.Clear();
#if UNITY_EDITOR
            _debugHistoryObjs.Clear();
#endif
        }

        // --- Core Cast Logic ---

        private void TryCast()
        {
            _prevHitPos = _currHitPos;
            var ray = _rayProvider.GetRay();
            CachedHits.Clear();
            _cachedRay.Value = ray;

            if (_cachedRay.Value.direction == Vector3.zero)
            {
                if (RuntimeDebugSetting.IsDebugMode)
                    Debug.LogWarning("Ray direction is zero, skipping cast.", this);
                return;
            }

            transform.rotation = Quaternion.LookRotation(_cachedRay.Value.direction);

            var currentRay = _cachedRay.Value;
            var distance = GetDistance();
            var endPoint = currentRay.origin + currentRay.direction * distance;

            if (_cacheOrigin != null)
                _cacheOrigin.position = currentRay.origin;
            if (_cacheEndPoint != null)
                _cacheEndPoint.position = endPoint;
            if (_isDrawDebugColor)
                Debug.DrawLine(currentRay.origin, endPoint, _overrideGizmoColor, 10f);

            var hitCount = PerformCast(currentRay, distance, _castResultsBuffer);

            // 先濾掉忽略對象，再排序：少排幾個，也避免被忽略的 collider 佔住 _singleHitOnly 的名額
            hitCount = FilterIgnoredHits(_castResultsBuffer, hitCount);

            // 依距離由近到遠排序
            // 不能用 Array.Sort(comparer)：comparer 會被 box 成 IComparer<RaycastHit>，每幀都產生 GC
            if (hitCount > 1)
                SortByDistance(_castResultsBuffer, hitCount);

            var actualCount = _singleHitOnly ? Mathf.Min(hitCount, 1) : hitCount;

            // 更新 hit collider list（Clear + Add 無 GC）
            _hitColliders.Clear();
            for (var i = 0; i < actualCount; i++)
                _hitColliders.Add(_castResultsBuffer[i].collider);

            if (actualCount <= 0)
            {
                var farPoint = currentRay.origin + currentRay.direction * distance;
                _hitPosVar?.SetValue(farPoint);
                if (_hitPosVarTransform != null && _hitPosVarTransform.Value != null)
                    _hitPosVarTransform.Value.position = farPoint;
                _hasHitVar?.SetValue(false);
            }
            else
            {
                for (var i = 0; i < actualCount; i++)
                    CachedHits.Add(_castResultsBuffer[i]);

                var firstHit = _castResultsBuffer[0];
                _hitPosVar?.SetValue(firstHit.point);
                if (_hitPosVarTransform != null && _hitPosVarTransform.Value != null)
                    _hitPosVarTransform.Value.position = firstHit.point;
                _hasHitVar?.SetValue(true);
            }

#if UNITY_EDITOR
            if (RuntimeDebugSetting.IsDebugMode)
            {
                var debugCollider = actualCount > 0 ? _castResultsBuffer[0].collider : null;
                _debugHistoryObjs.Enqueue(debugCollider);
                if (_debugHistoryObjs.Count > 10)
                    _debugHistoryObjs.Dequeue();
            }

#endif
        }

        // --- Ignore Filter ---

        /// <summary>
        ///     把被忽略的 hit 從 buffer 中原地移除（保持順序），回傳剩下的數量。零 GC。
        /// </summary>
        private int FilterIgnoredHits(RaycastHit[] array, int count)
        {
#if UNITY_EDITOR
            _debugIgnoredHitCount = 0;
#endif
            if (count <= 0 || !_ignoreFilter.HasIgnoreTarget)
                return count;

            var write = 0;
            for (var i = 0; i < count; i++)
            {
                if (_ignoreFilter.IsIgnored(array[i].collider))
                {
#if UNITY_EDITOR
                    _debugIgnoredHitCount++;
#endif
                    continue;
                }

                if (write != i)
                    array[write] = array[i];
                write++;
            }

            return write;
        }

        /// <summary>
        ///     依 distance 由近到遠 in-place insertion sort，零 GC（count 最多 20，insertion sort 足夠）。
        /// </summary>
        private static void SortByDistance(RaycastHit[] array, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var key = array[i];
                var j = i - 1;
                while (j >= 0 && array[j].distance > key.distance)
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = key;
            }
        }

        /// <summary>
        ///     子類實作實際的物理 cast。回傳命中數量。
        /// </summary>
        protected abstract int PerformCast(Ray ray, float distance, RaycastHit[] results);

        // --- Gizmo ---

        private void OnDrawGizmos()
        {
            if (!enabled)
                return;
            Gizmos.color = _overrideGizmoColor;

            if (Application.isPlaying == false && _rayProvider != null)
                _cachedRay.SimValue = _rayProvider.GetRay();

            var ray = _cachedRay.Value;
            Gizmos.DrawRay(ray.origin, ray.direction * GetDistance());
            Gizmos.DrawWireCube(ray.origin, Vector3.one * 0.1f);

            DrawCastGizmo(ray, GetDistance());

            if (CachedHits != null)
                foreach (var hit in CachedHits)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(hit.point, 0.1f);
                }
        }

        /// <summary>
        ///     子類可 override 來畫額外的 Gizmo（如 SphereCast 的球體）。
        /// </summary>
        protected virtual void DrawCastGizmo(Ray ray, float distance)
        {
        }

        // --- Info ---

        // public override string Description => _rayProvider?.GetType().Name;

#if UNITY_EDITOR
        public override string ValueInfo => "layer:" + _hittingLayer.value;
        public override bool IsDrawingValueInfo => true;
#endif
    }
}
