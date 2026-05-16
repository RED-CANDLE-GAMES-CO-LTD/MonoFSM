using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    public class ViewRoot : AbstractDescriptionBehaviour, ISceneStart, IUpdateSimulate,
        IAfterRenderMono
    {
        [FormerlySerializedAs("_ignoreReparent")] [SerializeField]
        private bool _ignoreStartReparent; //dynamic的應該要 ignore?
        [PreviewInInspector] [AutoParent] private Animator _animator;

        // 相對 _parentViewRoot 的 offset（Start 綁定 or SetFollowTarget 動態設定）
        [ShowInInspector] Vector3 _followParentOffset; // Root 相對 parentVR.Root（Simulate 用）
        [ShowInInspector] Quaternion _followParentRotOffset;
        [ShowInInspector] Vector3 _followViewOffset; // View 相對 parentVR.transform（AfterRender 用）
        [ShowInInspector] Quaternion _followViewRotOffset;

        public Transform Root => transform.parent;

        protected override bool IsIgnoreRename => true;

        // protected override void Awake()
        // {
        //     base.Awake();
        //
        // }

        protected override void Start()
        {
            base.Start();
            // Debug.Log($"[ViewRoot] Awake: '{name}'", this);
            _animator.keepAnimatorStateOnDisable = true; //保持動畫狀態，避免重啟後閃回default state
            _animator.writeDefaultValuesOnDisable = false;
            // ReparentToRoot(); //這裡沒問題喔
        }


        [ShowInInspector] private Transform _entityParentTransform;
        [ShowInInspector] bool _sceneStarted = false;
        public void EnterSceneStart()
        {
            if (_ignoreStartReparent)
            {
                // enabled = false;
                return;
            }

            //這裡做失敗QQ, 沒搞懂？ worldUpdateSimulator比全部都早？(singleton?) 所以應該是要有個場景上的物件管理start load完？
            _entityParentTransform = BindEntity.transform.parent;
            if (_entityParentTransform == null) return;

            //FIXME: client竟然會關著？行為不一致？
            var parentEntity = _entityParentTransform.GetComponentInParent<MonoEntity>(true);
            if (parentEntity == null)
            {
                return;
            }
            _parentEntity = parentEntity;
            _parentViewRoot = _parentEntity?.ViewRoot;
            RecordOffsets(_parentViewRoot);

            _sceneStarted = true;
        }

        // 用當前世界座標記錄相對 target 的 offset（Root 用 Simulate、View 用 AfterRender）
        void RecordOffsets(ViewRoot target)
        {
            _followParentOffset = target.Root.InverseTransformPoint(Root.position);
            _followParentRotOffset = Quaternion.Inverse(target.Root.rotation) * Root.rotation;
            _followViewOffset = target.transform.InverseTransformPoint(transform.position);
            _followViewRotOffset = Quaternion.Inverse(target.transform.rotation) * transform.rotation;
        }

        [ShowInInspector]
        MonoEntity _parentEntity;


        // [Button]
        // void ReparentToRoot()
        // {
        //     if (_ignoreReparent) return;
        //
        //
        //
        //     // 找 parent entity 自己的 ViewRoot（排除 nested entity 的）
        //     ViewRoot parentViewRoot = null;
        //     foreach (var vr in _parentEntity.GetComponentsInChildren<ViewRoot>(true))
        //     {
        //         if (vr.GetComponentInParent<MonoEntity>() == _parentEntity)
        //         {
        //             parentViewRoot = vr;
        //             break;
        //         }
        //     }
        //
        //     if (parentViewRoot == null)
        //     {
        //         Debug.LogWarning($"[ViewRoot] Parent '{_parentEntity.name}' has no ViewRoot", this);
        //         return;
        //     }
        //
        //     if (_animator.isInitialized == false)
        //     {
        //         Debug.LogError(
        //             $"[ViewRoot] Animator on '{name}' is not initialized. Make sure it has a valid controller and is enabled at least once before scene start.",
        //             this);
        //         // Debug.Break();
        //         _animator.Rebind();
        //     }
        //
        //     if (_animator.keepAnimatorStateOnDisable == false)
        //     {
        //         Debug.LogError(
        //             $"[ViewRoot] Animator on '{name}' does not have 'Keep Animator State On Disable' enabled. This may cause animation issues after reparenting.",
        //             this);
        //         Debug.Break();
        //     }
        //
        //     // worldPositionStays = true 保持世界座標不變
        //     //小心！動畫裡有key到ViewRoot的position/rotation的話 reparent 會跑掉
        //     transform.SetParent(parentViewRoot.transform, true);
        //
        //
        //     // Debug.Log(
        //     //     $"[ViewRoot] Reparented '{ParentEntity.name}' ViewRoot under '{parentEntity.name}' ViewRoot",
        //     //     this);
        // }

        [ShowInPlayMode] public ViewRoot _parentViewRoot; //

        #region FollowTarget 掛載

        public void SetFollowTarget(ViewRoot target, Vector3 mountPosition,
            Quaternion mountRotation)
        {
            _parentViewRoot = target;
            // 先把 Root (Animator/Rigidbody) 移到指定位置
            if (Root != null)
            {
                Root.position = mountPosition;
                Root.rotation = mountRotation;
            }
            else
            {
                Debug.LogError(
                    $"[ViewRoot] '{name}' has no Root to follow target. FollowTarget mode requires the ViewRoot to have a parent (e.g. Animator).",
                    this);
            }

            RecordOffsets(target);
        }

        public void ClearFollowTarget()
        {
            // Debug.Log($"[ViewRoot] '{name}' cleared follow target.", this);
            _parentViewRoot = null;
        }

        #endregion

        public void Simulate(float deltaTime)
        {
            var parentVR = _parentViewRoot;
            if (parentVR == null) return;
            Root.position = parentVR.Root.TransformPoint(_followParentOffset);
            Root.rotation = parentVR.Root.rotation * _followParentRotOffset;
        }

        [ShowInInspector] float _lastRenderTick = -1;
        public void AfterRender()
        {
            // ViewRoot 做 localOffset 同步（interpolated）
            // 統一處理 Nested ViewRoot / FollowTarget (Dock, Socket) 兩種情境
            if (_parentViewRoot == null) return;
            transform.position = _parentViewRoot.transform.TransformPoint(_followViewOffset);
            transform.rotation = _parentViewRoot.transform.rotation * _followViewRotOffset;
            _lastRenderTick = WorldUpdateSimulator.CurrentTick;
        }
    }
}
