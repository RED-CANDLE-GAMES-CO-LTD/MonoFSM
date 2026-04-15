using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Detection
{
    public struct DetectionResult
    {
        // public Rigidbody _targetRigidbody; //為了不要重複判斷？
        public BaseEffectDetectTarget targetObject; //collider或是rigidbody?
        public GameObject _target;
        public Vector3? hitPoint;
        public Vector3? hitNormal;
        public bool isValidHit;

        public DetectionResult(
            // Rigidbody targetRigidbody,
            GameObject target, //為何不用collider?
            Vector3? hitPoint = null,
            Vector3? hitNormal = null
        )
        {
            // this._targetRigidbody = targetRigidbody;
            _target = target;
            if (target.TryGetComponent<BaseEffectDetectTarget>(out var detectTarget))
                targetObject = detectTarget;
            else
                targetObject = null;
            this.hitPoint = hitPoint;
            this.hitNormal = hitNormal;
            this.isValidHit = targetObject != null;
        }

        public static DetectionResult Invalid => new DetectionResult { isValidHit = false };
    }

    // public interface IDetectionSource //AbstractComponent
    public abstract class AbstractDetectionSource
        : AbstractDescriptionBehaviour,
            IHierarchyValueInfo
    {
        protected override string DescriptionTag => "DetectionSource";

        [Required]
        [AutoParent]
        EffectDetector _detector;

        // public virtual bool IsEnabled => isActiveAndEnabled;

        //統一由 EffectDetector 管理 enter/exit 事件

        //FIXME: 要有thisFrameHitData?
        //FIXME: 已經打死是Collider了，感覺要抽象掉一層，3D間共用？傳T?
        [PreviewInInspector]
        protected readonly HashSet<Collider> _thisFrameColliders = new(); //這個有用嗎？

        [ShowInDebugMode]
        protected List<DetectionResult> _buffer = new();

        // [PreviewInDebugMode]
        // protected readonly HashSet<Collider> _lastFrameColliders = new(); //ondisable也要清掉？

        private void OnDisable()
        {
            //FIXME: 要讓 EffectDetector handle就好？
            //TODO: Exit check?
            _thisFrameColliders.Clear();
            // _lastFrameColliders.Clear();
        }

        public abstract List<DetectionResult> GetCurrentDetections();

        public virtual void UpdateDetection()
        {
            // DetectionSource 只負責檢測，事件處理由 EffectDetector 統一管理
        }

        public virtual void AfterDetection() { }

        public override string ValueInfo => "L:" + LayerMask.LayerToName(gameObject.layer);
        public override bool IsDrawingValueInfo => true;
    }
}
