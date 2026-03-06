using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    public class ViewRoot : AbstractDescriptionBehaviour, ISceneStart
    {
        [SerializeField] private bool _ignoreReparent;
        [PreviewInInspector] [AutoParent] private Animator _animator;

        protected override bool IsIgnoreRename => true;

        protected override void Awake()
        {
            base.Awake();
            // Debug.Log($"[ViewRoot] Awake: '{name}'", this);
            _animator.keepAnimatorStateOnDisable = true; //保持動畫狀態，避免重啟後閃回default state
        }

        public void EnterSceneStart()
        {
            //這裡做失敗QQ, 沒搞懂？ worldUpdateSimulator比全部都早？(singleton?) 所以應該是要有個場景上的物件管理start load完？
        }

        protected override void Start()
        {
            base.Start();
            ReparentToRoot(); //這裡沒問題喔
        }

        [Button]
        void ReparentToRoot()
        {
            if (_ignoreReparent) return;

            var parentTransform = ParentEntity.transform.parent;
            if (parentTransform == null) return;

            var parentEntity = parentTransform.GetComponentInParent<MonoEntity>();
            if (parentEntity == null) return;

            // 找 parent entity 自己的 ViewRoot（排除 nested entity 的）
            ViewRoot parentViewRoot = null;
            foreach (var vr in parentEntity.GetComponentsInChildren<ViewRoot>(true))
            {
                if (vr.GetComponentInParent<MonoEntity>() == parentEntity)
                {
                    parentViewRoot = vr;
                    break;
                }
            }

            if (parentViewRoot == null)
            {
                Debug.LogWarning($"[ViewRoot] Parent '{parentEntity.name}' has no ViewRoot", this);
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
    }
}
