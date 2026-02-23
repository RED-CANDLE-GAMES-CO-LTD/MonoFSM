using System;
using System.Collections.Generic;
using MonoFSM_Physics.Runtime.Interact.SpatialDetection;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
using MonoFSM.PhysicsWrapper;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    /// <summary>
    ///     純做Raycast的偵測器，會在SimulateUpdate時進行射線檢測。
    /// </summary>
    [DefaultExecutionOrder(-1)] //要把RaycastDetectSource前面，有執行順序問題hmm
    public class RaycastCache
        : AbstractDescriptionBehaviour,
            IBeforeSimulate,
            IUpdateSimulate,
            IResetStateRestore,
            IHierarchyValueInfo, IRenderSimulate, ISceneAwake

    {
        [SerializeField]
        private Transform _cacheOrigin;

        [SerializeField]
        private Transform _cacheEndPoint;

        public enum RaycastMode
        {
            Single, //FIXME: 應該都要用all 然後再sort, 然後會需要filter掉一部分
            All //會需要all嗎？這樣對象要全部分開？
            ,
        }

        [SerializeField]
        private RaycastMode _raycastMode = RaycastMode.Single;

        bool IsHavingDistanceSource => _distanceSource != null;

        [HideIf(nameof(IsHavingDistanceSource))]
        public float _distance = 30; //要依照速度來決定distance...distance provider?

        [FormerlySerializedAs("_distanceProvider")]
        [CompRef]
        [Auto]
        [SerializeField]
        private DistanceSourceFromSpeed _distanceSource;

        [ShowInInspector]
        private float GetDistance()
        {
            if (_distanceSource != null)
                return _distanceSource.Distance;
            return _distance;
        }

        [FormerlySerializedAs("HittingLayer")]
        [CustomSerializable]
        // [ShowInInspector]
        // [Required]
        public LayerMask _hittingLayer;

        //FIXME: validate 不可以是nothing? 或是直接收斂掉？

        private RaycastHit[] _allocHits = new RaycastHit[10]; //FIXME: 這個大小要怎麼處理？會不會有問題？ 這個是用來儲存raycast的結果

        private Collider[] _allocColliders = new Collider[10]; //FIXME: 這個大小要怎麼處理？會不會有問題？ 這個是用來儲存raycast的結果

        //用spherecast還是raycast？ spherecast會有問題嗎？

        [PreviewInInspector]
        private Collider firstHitCollider =>
            CachedHits is { Count: > 0 } ? CachedHits[0].collider : null;

        private DualPhaseValue<List<RaycastHit>> _cachedHits = new(); //這個是用來儲存raycast的結果

        [PreviewInInspector] public List<RaycastHit> CachedHits => _cachedHits.Value;

        public RaycastHit CachedHit => CachedHits is { Count: > 0 } ? CachedHits[0] : default;
        public VarVector3 _hitPosVar;
        public Ray CachedRay => _cachedRay.Value;

        private IRaycastProcessor raycastProcessor =>
            _parentObj.WorldUpdateSimulator.GetCompCache<IRaycastProcessor>();

        // [CompRef] //all in 1 就撞了？
        // [Auto]
        // private ISphereCastProcessor _sphereCastProcessor;
        // public float _sphereRadius = 0.5f; //FIXME: spherecast的半徑要怎麼處理？ 這個是用來儲存spherecast的結果
        [ShowInInspector] private DualPhaseValue<Ray> _cachedRay = new();

#if UNITY_EDITOR
        [ShowInDebugMode]
        private readonly Queue<Collider> _debugHistoryObjs = new();
#endif

        public bool _isDrawDebugColor;

        [SerializeField]
        private Color _overrideGizmoColor = Color.red;

        private void OnDrawGizmos()
        {
            if (!enabled)
                return;
            Gizmos.color = _overrideGizmoColor;

            //FIXME: 處理 editor mode的ray provider
            if (Application.isPlaying == false && _rayProvider != null)
            {
                _cachedRay.SimValue = _rayProvider.GetRay();
            }

            // Debug.Log("[RaycastCache] Draw Gizmo Ray:" + _cachedRay, this);
            //FIXME: 要選mode? sphere cast, ray cast...
            var ray = _cachedRay.Value;
            Gizmos.DrawRay(ray.origin, ray.direction * GetDistance());
            Gizmos.DrawWireCube(ray.origin, Vector3.one * 0.1f);
            if (CachedHits != null)
                foreach (var hit in CachedHits)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(hit.point, 0.1f);
                }
        }

        public QueryTriggerInteraction _queryTriggerInteraction = QueryTriggerInteraction.Collide;

        private void TryCast()
        {
            var ray = _rayProvider.GetRay();
            CachedHits.Clear();
            _cachedRay.Value = ray;
            transform.rotation = Quaternion.LookRotation(_cachedRay.Value.direction);
            if (_raycastMode == RaycastMode.Single)
            {
                var currentRay = _cachedRay.Value;
                var endPoint = currentRay.origin + currentRay.direction * GetDistance();
                if (_cacheOrigin != null)
                    _cacheOrigin.position = currentRay.origin;
                if (_cacheEndPoint != null)
                    _cacheEndPoint.position = endPoint;
                if (_isDrawDebugColor)
                    Debug.DrawLine(currentRay.origin, endPoint, _overrideGizmoColor, 10f);

                var result = raycastProcessor.Raycast(
                    ray.origin,
                    ray.direction,
                    out var hitInfo,
                    GetDistance(),
                    _hittingLayer,
                    _queryTriggerInteraction
                );
                if (!result)
                {
                    // 沒打到 collider 時，用 ray 方向上最大距離的位置當作 hit point
                    hitInfo = new RaycastHit();
                    var farPoint = currentRay.origin + currentRay.direction * GetDistance();
                    // RaycastHit.point 是 readonly，透過反射或直接用 unsafe 不太好，
                    // 改用 _hitPosVar 記錄位置，並把 endPoint 更新
                    _hitPosVar?.SetValue(farPoint);
                }
                else
                {
                    _hitPosVar?.SetValue(hitInfo.point);
                }
                //FIXME: 操作 list好嗎？
                CachedHits.Add(hitInfo);
                // Debug.Log("[RaycastCache] RaycastProcessor Hit:" + hitInfo.collider, this);
                // _thisFrameColliders.Add(hitInfo.collider);
#if UNITY_EDITOR
                _debugHistoryObjs.Enqueue(hitInfo.collider);
                if (_debugHistoryObjs.Count > 10)
                    _debugHistoryObjs.Dequeue();
#endif
            }
            else
            {
                throw new ArgumentNullException($"Not implement Multiple Raycast");
            }
            // else
            // {
            //     var hits = Physics.RaycastAll(ray, _distance, HittingLayer);
            //     foreach (var h in hits)
            //     {
            //         _cachedHits.Add(h);
            //         _thisFrameColliders.Add(h.collider);
            //         Debug.Log("hit" + h.collider.name, h.collider);
            //     }
            // }
        }

        [Required]
        [Auto]
        [CompRef]
        private AbstractRayProvider _rayProvider;
        // private float _deltaTime;

        //FIXME: raycast時間點...
        //beforeStateUpdate?
        //AfterStateUpdate?
        //怎麼保證這幾個順序？寫在StateUpdate裡一起用？

        public bool _manualUpdateMode; //FIXME: 這個要不要放在外面？ 讓外面控制

        //直接放在variable下面也是蠻好笑的？
        public void Simulate(float deltaTime) //這個優先順序問題？
        {
            // _deltaTime = deltaTime;
            TryCast();
        }

        protected override string DescriptionTag => "Raycast";
        public override string Description => _rayProvider?.GetType().Name;

        public void ResetStateRestore()
        {
            //把狀態清掉
            _cachedRay.Reset();

            _cachedHits.SimValue?.Clear();
            _cachedHits.RenderValue?.Clear();
#if UNITY_EDITOR
            _debugHistoryObjs.Clear();
#endif
        }


        public void BeforeSimulate(float deltaTime)
        {

            // _deltaTime = deltaTime;

            // Debug.Log("[RaycastCache] BeforeSimulate Ray:" + _cachedRay, this);
        }

#if UNITY_EDITOR
        public string ValueInfo => "layer:" + _hittingLayer.value; //FIXME: 可能會是多個..
        public bool IsDrawingValueInfo => true;
#endif
        public void Render(float deltaTime)
        {
            //fIXME: 從 camera來的 ray, 一定很晚，所以這邊都是上個 frame的？但上個frame會怎麼樣嗎？
            // TryCast();

            // Debug.Log("[RaycastCache] Render Ray:" + _cachedRay.RenderValue, this);
        }

        private void Update()
        {
            // TryCast();
        }

        private void LateUpdate()
        {
            // TryCast();
        }

        public void EnterSceneAwake()
        {
            _cachedHits.SimValue = new List<RaycastHit>();
            _cachedHits.RenderValue = new List<RaycastHit>();
        }
    }

    public abstract class AbstractRayProvider : MonoBehaviour
    {
        //FIXME: 先判定需要才算？ IsValid?
        public abstract Ray GetRay();
        //FIXME: 應該要包含距離？
    }
}
