using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    public class ViewRoot : AbstractDescriptionBehaviour, ISceneStart, IAfterRenderMono
    {
        [SerializeField] private bool _ignoreReparent;
        [PreviewInInspector] [AutoParent] private Animator _animator;

        protected override bool IsIgnoreRename => true;

        protected override void Awake()
        {
            base.Awake();
            // Debug.Log($"[ViewRoot] Awake: '{name}'", this);
            _animator.keepAnimatorStateOnDisable = true; //保持動畫狀態，避免重啟後閃回default state
            _animator.writeDefaultValuesOnDisable = false;
        }

        public void EnterSceneStart()
        {
            if (_ignoreReparent)
            {
                enabled = false;
                return;
            }

            //這裡做失敗QQ, 沒搞懂？ worldUpdateSimulator比全部都早？(singleton?) 所以應該是要有個場景上的物件管理start load完？
            var parentTransform = SelfEntity.transform.parent;
            if (parentTransform == null) return;

            var parentEntity = parentTransform.GetComponentInParent<MonoEntity>();
            if (parentEntity == null)
            {
                // Debug.LogError(
                //     $"[ViewRoot] Parent of '{name}' is not part of a MonoEntity. ViewRoot requires a parent MonoEntity to function properly.",
                //     this);
                return;
            }

            _parentEntity = parentEntity;

            // 記錄相對於 parentViewRoot 的 offset（模擬 child 跟隨）
            var parentVR = parentViewRoot;
            if (parentVR != null)
            {
                _offsetPosition = parentVR.InverseTransformPoint(transform.position);
                _offsetRotation = Quaternion.Inverse(parentVR.rotation) * transform.rotation;
            }
        }

        MonoEntity _parentEntity;
        [ShowInInspector] Vector3 _offsetPosition;
        [ShowInInspector] Quaternion _offsetRotation;
        protected override void Start()
        {
            base.Start();
            // ReparentToRoot(); //這裡沒問題喔
        }

        [Button]
        void ReparentToRoot()
        {
            if (_ignoreReparent) return;



            // 找 parent entity 自己的 ViewRoot（排除 nested entity 的）
            ViewRoot parentViewRoot = null;
            foreach (var vr in _parentEntity.GetComponentsInChildren<ViewRoot>(true))
            {
                if (vr.GetComponentInParent<MonoEntity>() == _parentEntity)
                {
                    parentViewRoot = vr;
                    break;
                }
            }

            if (parentViewRoot == null)
            {
                Debug.LogWarning($"[ViewRoot] Parent '{_parentEntity.name}' has no ViewRoot", this);
                return;
            }

            if (_animator.isInitialized == false)
            {
                Debug.LogError(
                    $"[ViewRoot] Animator on '{name}' is not initialized. Make sure it has a valid controller and is enabled at least once before scene start.",
                    this);
                // Debug.Break();
                _animator.Rebind();
            }

            if (_animator.keepAnimatorStateOnDisable == false)
            {
                Debug.LogError(
                    $"[ViewRoot] Animator on '{name}' does not have 'Keep Animator State On Disable' enabled. This may cause animation issues after reparenting.",
                    this);
                Debug.Break();
            }

            // worldPositionStays = true 保持世界座標不變
            //小心！動畫裡有key到ViewRoot的position/rotation的話 reparent 會跑掉
            transform.SetParent(parentViewRoot.transform, true);


            // Debug.Log(
            //     $"[ViewRoot] Reparented '{ParentEntity.name}' ViewRoot under '{parentEntity.name}' ViewRoot",
            //     this);
        }

        [ShowInPlayMode] public Transform parentViewRoot => _parentEntity?.ViewRoot?.transform;

        public void AfterRender()
        {
            var parentVR = parentViewRoot;
            if (parentVR == null) return;

            transform.position = parentVR.TransformPoint(_offsetPosition);
            transform.rotation = parentVR.rotation * _offsetRotation;
        }

        private void LateUpdate()
        {
            // if (parentViewRoot == null) return;
            // transform.localPosition = parentViewRoot.localPosition;
            // transform.localRotation = parentViewRoot.localRotation;
        }
    }
}
