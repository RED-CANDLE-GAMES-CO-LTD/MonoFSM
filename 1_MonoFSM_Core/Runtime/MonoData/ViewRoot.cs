using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    /// <summary>
    /// 跟隨parent 的 view
    /// </summary>
    public class ViewRoot : AbstractDescriptionBehaviour, ISceneStart, IUpdateSimulate,
        IAfterRenderMono, IResetStateRestore
    {
        [Tooltip("物理物件不該一開始被reparet")] [FormerlySerializedAs("_ignoreReparent")] [SerializeField]
        private bool _ignoreStartReparent; //dynamic的應該要 ignore?
        [PreviewInInspector] [AutoParent] private Animator _animator;

        // 相對 _parentViewRoot 的 offset（Start 綁定 or SetFollowTarget 動態設定）
        [ShowInInspector] Vector3 _followParentOffset; // Root 相對 parentVR.Root（Simulate 用）
        [ShowInInspector] Quaternion _followParentRotOffset;
        [ShowInInspector] Vector3 _followViewOffset; // View 相對 parentVR.transform（AfterRender 用）
        [ShowInInspector] Quaternion _followViewRotOffset;

        //FIXME: 要檢查上面有Rigidbody?
        public Transform Root =>
            _bindRb != null
                ? _bindRb.transform
                : _bindAnim != null
                    ? _bindAnim.transform
                    : _bindRb.transform.parent;

        [AutoParent] private Animator _bindAnim;
        [AutoParent] private Rigidbody _bindRb;
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

            _attachToEntityWrapper.SetValue(parentEntity, this);
            // _parentViewRoot = _parentEntity?.ViewRoot;
            RecordOffsets(AttachToViewRoot);

            // 把 SceneStart 的 attach 當成 baseline 快照，ResetStateRestore 還原用
            SnapshotSceneStartBaseline(parentEntity);

            _sceneStarted = true;
        }

        // === ResetStateRestore baseline（只記 SceneStart 時 mount 的，動態 mount 不記）===
        [ShowInInspector] private MonoEntity _sceneStartAttachEntity;
        Vector3 _baselineParentOffset;
        Quaternion _baselineParentRotOffset;
        Vector3 _baselineViewOffset;
        Quaternion _baselineViewRotOffset;

        void SnapshotSceneStartBaseline(MonoEntity attachEntity)
        {
            _sceneStartAttachEntity = attachEntity;
            _baselineParentOffset = _followParentOffset;
            _baselineParentRotOffset = _followParentRotOffset;
            _baselineViewOffset = _followViewOffset;
            _baselineViewRotOffset = _followViewRotOffset;
        }

        // 用當前世界座標記錄相對 target 的 offset（Root 用 Simulate、View 用 AfterRender）
        void RecordOffsets(ViewRoot target)
        {
            _followParentOffset = target.Root.InverseTransformPoint(Root.position);
            _followParentRotOffset = Quaternion.Inverse(target.Root.rotation) * Root.rotation;

            //FIXME: 為什麼 view 的 offset 要分開？
            _followViewOffset = target.transform.InverseTransformPoint(transform.position);
            _followViewRotOffset = Quaternion.Inverse(target.transform.rotation) * transform.rotation;
        }

        public VarEntityWrapper _attachToEntityWrapper;

        //又不想寫特歸code? VarViewRoot? 帶了Component type,
        //最佳解是什麼？
        [ShowInPlayMode] public ViewRoot AttachToViewRoot => _attachToEntityWrapper.Value?.ViewRoot;
        // public VarComp _followViewRootVar; //為了連線?, 有點髒...



        #region FollowTarget 掛載

        [ShowInInspector] private Transform _mountPointTarget; //連線同步走 NetworkedViewRoot（MountPointRegistry index）

        // 給 NetworkedViewRoot 讀取當前 mount 狀態用
        public MonoEntity AttachToEntity => _attachToEntityWrapper.Value;
        public Transform MountPointTarget => _mountPointTarget;
        public Vector3 FollowParentOffset => _followParentOffset;
        public Quaternion FollowParentRotOffset => _followParentRotOffset;
        public Vector3 FollowViewOffset => _followViewOffset;
        public Quaternion FollowViewRotOffset => _followViewRotOffset;
        public bool MountDisabledColliders => _mountDisabledColliders;

        [ShowInPlayMode] bool _mountHandledPhysics;
        [ShowInPlayMode] bool _mountDisabledColliders;

        /// <summary>
        /// Mount 的唯一入口（SA 端）：物理副作用 + collider + 跟隨狀態集中在這，
        /// NetworkedViewRoot 每 tick 讀取結果同步給其他端。
        /// </summary>
        public void MountTo(ViewRoot target, Vector3 mountPosition, Quaternion mountRotation,
            Transform mountPointTarget, bool handlePhysics, bool disableColliders)
        {
            if (handlePhysics && _bindRb != null)
            {
                _bindRb.isKinematic = true;
                _bindRb.linearVelocity = Vector3.zero;
                _bindRb.angularVelocity = Vector3.zero;
            }
            _mountHandledPhysics = handlePhysics;

            if (disableColliders)
                DisableCollidersForMount(BindEntity.transform);
            _mountDisabledColliders = disableColliders;

            SetFollowTarget(target, mountPosition, mountRotation, mountPointTarget);
        }

        /// <summary>
        /// Unmount 的唯一入口（SA 端）：還原物理 + collider + 清跟隨狀態。
        /// </summary>
        public void Unmount(bool handlePhysics)
        {
            if (handlePhysics && _bindRb != null)
                _bindRb.isKinematic = false;
            _mountHandledPhysics = false;

            RestoreCollidersAfterUnmount();
            _mountDisabledColliders = false;

            ClearFollowTarget();
        }

        /// <summary>
        /// 非 SA 端套用網路同步下來的 mount 狀態。
        /// 不碰 Rigidbody（proxy 的 kinematic 歸 NetworkRigidbody 管），只處理 collider 與跟隨狀態。
        /// offsets 直接用 SA 算好的值，不重新 RecordOffsets（proxy 當下的世界座標不可信）。
        /// </summary>
        public void ApplyMountFromNetwork(MonoEntity attachEntity, Transform mountPoint,
            Vector3 parentOffset, Quaternion parentRotOffset,
            Vector3 viewOffset, Quaternion viewRotOffset, bool disableColliders)
        {
            if (disableColliders && _collidersDisabledOnMount.Count == 0)
                DisableCollidersForMount(BindEntity.transform);
            else if (!disableColliders)
                RestoreCollidersAfterUnmount();
            _mountDisabledColliders = disableColliders;

            _attachToEntityWrapper.SetValue(attachEntity, this);
            _mountPointTarget = mountPoint;
            _followParentOffset = parentOffset;
            _followParentRotOffset = parentRotOffset;
            _followViewOffset = viewOffset;
            _followViewRotOffset = viewRotOffset;
        }

        /// <summary>非 SA 端套用網路同步下來的 unmount。</summary>
        public void ApplyUnmountFromNetwork()
        {
            RestoreCollidersAfterUnmount();
            _mountDisabledColliders = false;
            ClearFollowTarget();
        }

        //FIXME: rotation的處理？現在只有用相對位置
        public void SetFollowTarget(ViewRoot target, Vector3 mountPosition,
            Quaternion mountRotation, Transform mountPointTarget = null)
        {
            _attachToEntityWrapper.SetValue(target.BindEntity, this);
            _mountPointTarget = mountPointTarget;
            // AttachToViewRoot = target;
            // _followViewRootVar?.SetValue(target, this); //為了連線
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

            //為什麼要？
            RecordOffsets(target);
        }

        public void ClearFollowTarget()
        {
            // Debug.Log($"[ViewRoot] '{name}' cleared follow target.", this);
            _attachToEntityWrapper.ClearValue();
            _mountPointTarget = null;
        }

        // Mount 時被關掉的 colliders（Unmount 只還原這些，避免動到本來就 disabled 的）
        [ShowInPlayMode]
        readonly System.Collections.Generic.List<Collider> _collidersDisabledOnMount = new();

        /// <summary>
        /// 關掉 root 底下所有 enabled 的 collider，並記錄起來供 Unmount 還原
        /// </summary>
        public void DisableCollidersForMount(Transform root)
        {
            _collidersDisabledOnMount.Clear();
            foreach (var col in root.GetComponentsInChildren<Collider>())
            {
                if (!col.enabled) continue;
                col.enabled = false;
                _collidersDisabledOnMount.Add(col);
            }
        }

        /// <summary>
        /// 還原 Mount 時關掉的 colliders（沒記錄就是 no-op）
        /// </summary>
        public void RestoreCollidersAfterUnmount()
        {
            foreach (var col in _collidersDisabledOnMount)
                if (col != null)
                    col.enabled = true;
            _collidersDisabledOnMount.Clear();
        }

        #endregion

        public void Simulate(float deltaTime)
        {
            var parentVR = AttachToViewRoot;
            if (parentVR == null) return;

            if (_mountPointTarget != null)
            {
                Root.position = _mountPointTarget.position;
                Root.rotation = _mountPointTarget.rotation;
                return;
            }
            Root.position = parentVR.Root.TransformPoint(_followParentOffset);
            Root.rotation = parentVR.Root.rotation * _followParentRotOffset;
        }

        [ShowInInspector] float _lastRenderTick = -1;
        public void AfterRender()
        {
            // ViewRoot 做 localOffset 同步（interpolated）
            // 統一處理 Nested ViewRoot / FollowTarget (Dock, Socket) 兩種情境
            if (AttachToViewRoot == null) return;
            _lastRenderTick = WorldUpdateSimulator.CurrentTick;
            if (_mountPointTarget != null)
            {
                //FIXME: 這沒有跟到view吧...
                Root.position = _mountPointTarget.position;
                Root.rotation = _mountPointTarget.rotation;
                return;
            }
            transform.position = AttachToViewRoot.transform.TransformPoint(_followViewOffset);
            transform.rotation = AttachToViewRoot.transform.rotation * _followViewRotOffset;

        }

        public void ResetStateRestore(bool isHardReset)
        {
            // 還原 Mount 時關掉的 collider（沒記錄就是 no-op）
            RestoreCollidersAfterUnmount();

            if (_sceneStartAttachEntity != null)
            {
                // SceneStart 有 attach → 還原成 baseline（同時蓋掉任何動態 mount）
                _attachToEntityWrapper.SetValue(_sceneStartAttachEntity, this);
                _mountPointTarget = null;
                _followParentOffset = _baselineParentOffset;
                _followParentRotOffset = _baselineParentRotOffset;
                _followViewOffset = _baselineViewOffset;
                _followViewRotOffset = _baselineViewRotOffset;
            }
            else
            {
                // 純動態 mount（SceneStart 沒 attach）→ 清掉
                ClearFollowTarget();
            }
        }
    }
}
