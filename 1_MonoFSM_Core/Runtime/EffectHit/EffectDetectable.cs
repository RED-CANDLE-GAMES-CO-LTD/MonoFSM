using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.EffectHit;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Detection;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{


    //FIXME: 還是應該直接放在Animator上？
    [Searchable]
    [DisallowMultipleComponent]
    //BaseEffectDetectTarget 的 Group, 類似HitBoxRoot的感覺
    //從Detector過來
    public class EffectDetectable //這顆已經是Group了，反而不知道進入點耶
        : MonoDictFolder<GeneralEffectType, GeneralEffectReceiver>, IDefaultSerializable,
            IResetStateRestore //關係
    {
        protected override bool IsIgnoreRename => true;
        //可能不只一個？
        // [Obsolete("只是拿來新增用的button？其實不一定需要？")]

        //TODO: 如果想要永遠都把EffectDetectable打開，然後去關Collider (DetectHitBox?)要可以支援group node, 這樣就不是depth only 1了
        [CompRef]
        // [AutoChildren(DepthOneOnly = true)]
        [AutoChildren]
        [SerializeField]
        private BaseEffectDetectTarget[] _effectDetectTargets; //FIXME:不該？

        // [AutoParent] private StateMachineOwner owner;
        //
        // public StateMachineOwner Owner => owner;



        public GameObject TargetObject => gameObject;
        public bool IsValid => gameObject.activeInHierarchy && _interactConditions.IsAllValid();

        [AutoChildren] [CompRef]
        AbstractConditionBehaviour[]
            _conditions; //這個是要放在Detectable上的，還是DetectTarget上的？應該是前者？因為有些條件是整體的？
        //FIXME 這可以再包一層嗎？
        [AutoChildren]
        [CompRef]
        public AbstractEntityInteractCondition[] _interactConditions; //應該是可以有多個condition？

        public void CanBeInteractedBy(EffectDetector detector) //pre-assign?
        {
            foreach (var condition in _interactConditions)
            {
                condition._sourceEntity = detector.BindEntity;
                condition._targetEntity = BindEntity;
            }
            // return;
        }

        //DebugOnly
#if UNITY_EDITOR
        [GUIColor(1f, 0.5f, 0.5f)]
        [PreviewInInspector]
        public List<EffectDetector> _debugDetectors = new(); //沒在判？
#endif

        //FIXME: 要改成能支援photon 給的HitData？
        // public void ProcessEffectHit(EffectDetector detector, Vector3 hitPoint, Vector3 hitNormal)
        // {
        //     Debug.Log($"[EffectDetectable] ProcessEffectHit from {detector.name} to {name}", this);
        //     //FIXME: 在這邊new data...?

        protected override void AddImplement(GeneralEffectReceiver item) { }

        protected override void RemoveImplement(GeneralEffectReceiver item) { }

        protected override bool CanBeAdded(GeneralEffectReceiver item)
        {
            return true;
        }

        protected override string DescriptionTag => "-> EffectDetectable 接收";
        [AutoParent] Rigidbody _rb;
        public Rigidbody rb => _rb;

        public void ResetStateRestore()
        {
#if UNITY_EDITOR
            _debugDetectors.Clear();
#endif
        }

        public override void OnBeforePrefabSave()
        {
            base.OnBeforePrefabSave();
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col.isTrigger) //避免誤加
                    continue;
                if (col.GetComponentInParent<EffectDetector>() !=
                    null) //略過有EffectDetector父物件的Collider，避免誤加TriggerDetectableTarget
                {
                    if (col.TryGetComponent(out TriggerDetectableTarget detectableTarget))
                    {
                        Destroy(detectableTarget);
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(col);
#endif
                    }

                    continue;
                }

                if (col.TryGetCompOrAdd<TriggerDetectableTarget>())
                {
                }
            }
        }
    }
}
