using System.Collections.Generic;
using MonoDebugSetting;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using MonoFSM_Physics.Runtime.Interact.SpatialDetection;
using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
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
            IRenderSimulate,
            ISceneAwake
    {
        [SerializeField]
        protected Transform _cacheOrigin;

        [SerializeField]
        protected Transform _cacheEndPoint;

        protected bool IsHavingDistanceSource => _distanceSource != null;

        [HideIf(nameof(IsHavingDistanceSource))]
        public float _distance = 30;

        [FormerlySerializedAs("_distanceProvider")]
        [CompRef]
        [Auto]
        [SerializeField]
        private DistanceSourceFromSpeed _distanceSource;

        [ShowInInspector]
        public float GetDistance()
        {
            if (_distanceSource != null)
                return _distanceSource.Distance;
            return _distance;
        }

        [FormerlySerializedAs("HittingLayer")]
        [CustomSerializable]
        public LayerMask _hittingLayer;

        [PreviewInInspector]
        private Collider firstHitCollider =>
            CachedHits is { Count: > 0 } ? CachedHits[0].collider : null;

        private DualPhaseValue<List<RaycastHit>> _cachedHits = new();

        [PreviewInInspector] public List<RaycastHit> CachedHits => _cachedHits.Value;
        public RaycastHit CachedHit => CachedHits is { Count: > 0 } ? CachedHits[0] : default;

        public VarBool _hasHitVar;
        public VarVector3 _hitPosVar;
        public VarTransform _hitPosVarTransform;

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

        protected readonly RaycastHit[] _castResultsBuffer = new RaycastHit[20];

        Vector3 _prevHitPos;
        Vector3 _currHitPos;

        // --- Lifecycle ---

        public void EnterSceneAwake()
        {
            _cachedHits.SimValue = new List<RaycastHit>();
            _cachedHits.RenderValue = new List<RaycastHit>();
        }

        public void BeforeSimulate(float deltaTime)
        {
        }

        public void Simulate(float deltaTime)
        {
            TryCast();
        }

        public void Render(float deltaTime)
        {
        }

        public void ResetStateRestore()
        {
            _cachedRay.Reset();
            _cachedHits.SimValue?.Clear();
            _cachedHits.RenderValue?.Clear();
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
            var actualCount = _singleHitOnly ? Mathf.Min(hitCount, 1) : hitCount;

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
            var debugCollider = actualCount > 0 ? _castResultsBuffer[0].collider : null;
            _debugHistoryObjs.Enqueue(debugCollider);
            if (_debugHistoryObjs.Count > 10)
                _debugHistoryObjs.Dequeue();
#endif
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

        public override string Description => _rayProvider?.GetType().Name;

#if UNITY_EDITOR
        public override string ValueInfo => "layer:" + _hittingLayer.value;
        public override bool IsDrawingValueInfo => true;
#endif
    }
}
