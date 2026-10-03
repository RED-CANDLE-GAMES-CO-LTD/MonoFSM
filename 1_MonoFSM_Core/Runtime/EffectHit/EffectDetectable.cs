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

        //TODO: 如果想要永遠都把EffectDetectable打開，然後去關Collider (DetectHitBox?)要可以支援group node, 這樣就不是depth only 1了
        [CompRef]
        // [AutoChildren(DepthOneOnly = true)]
        // [AutoChildren]
        [AutoChildren(StopAtType = typeof(EffectDetectable))]
        [SerializeField]
        private BaseEffectDetectTarget[] _effectDetectTargets; //FIXME:不該？

        /// <summary>這顆 detectable 底下（不跨進下一層 EffectDetectable）的 detect target，要拿實際 collider 做幾何計算時用</summary>
        public BaseEffectDetectTarget[] EffectDetectTargets => _effectDetectTargets;

        public GameObject TargetObject => gameObject;
        public bool IsValid => gameObject.activeInHierarchy && _interactConditions.IsAllValid();

        //被 culling handle 暫停（不含 despawn／手動關掉）：detector 端據此凍結、不發 exit/enter
        public bool IsSuspendedByCulling => _parentObj != null && _parentObj.IsCulledByHandle;

        // [AutoChildren(DepthOneOnly = true)] [CompRef]
        // AbstractConditionBehaviour[]
        //     _conditions; //這個是要放在Detectable上的，還是DetectTarget上的？應該是前者？因為有些條件是整體的？
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

        /// <summary>
        /// 同一個 effectType 在本地 / 多顆 external dict（併進來的 ModulePack）都有 receiver 時，原本是「先找到的贏」。
        /// 這裡讓 inactive 的那顆（「關掉 = 設 inactive」留著當註解的舊 receiver）讓給後面 active 的同 type receiver，
        /// 跟 VariableFolder 撞 tag 時 active 贏是同一個慣例。
        /// 正常路徑（第一顆就是 active）只多一次 activeInHierarchy；detectable 自己 inactive（pool 中）就照舊回第一顆。
        /// </summary>
        public override GeneralEffectReceiver Get(GeneralEffectType key)
        {
            var first = base.Get(key);
            if (first == null || first.gameObject.activeInHierarchy || !gameObject.activeInHierarchy)
                return first;

            foreach (var dict in _externalDicts)
            {
                if (dict == null) continue;
                var found = dict.Get(key);
                if (found != null && found.gameObject.activeInHierarchy)
                    return found;
            }

            return first;
        }

        protected override void AddImplement(GeneralEffectReceiver item) { }

        protected override void RemoveImplement(GeneralEffectReceiver item) { }

        protected override bool CanBeAdded(GeneralEffectReceiver item)
        {
            return true;
        }

        protected override string DescriptionTag => "-> EffectDetectable 接收";

        [AutoParent] private Rigidbody _rb;
        // public Rigidbody rb => _rb;

        public void ResetStateRestore(bool isHardReset)
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
                if (col.isTrigger || !col.enabled) //避免誤加
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

                if (col.enabled && col.TryGetCompOrAdd<TriggerDetectableTarget>())
                {
                }
            }
        }
    }
}
