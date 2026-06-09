using System;
using MonoFSM.Core;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.TransformAction
{
    //FIXME: state exit要收回 (對稱性)
    //FIXME: 改用VarTransform?
    [Obsolete]
    public class SetTransformParentAction : AbstractStateLifeCycleHandler
    {
        private Transform _oriParent; //FIXME: 不對..

        // public Transform _target;
        public override string Description =>
            $"Set {_sourceValueProvider?.Description} as child of {_targetValueProvider?.Description}";

        [ValueTypeValidate(typeof(Transform))]
        [DropDownRef]
        public ValueProvider _sourceValueProvider; //FIXME: missing的時候validate有問題捏

        // public ValueProvider<Transform> Test; //FIXME: 感覺錯了

        // public VarCompProviderRef _targetVarRef; //拿到rigidbody的話，再拿transform
        [ValueTypeValidate(typeof(Transform))] //FIXME: validation沒有辦法提示嗎
        [DropDownRef]
        public ValueProvider _targetValueProvider;

        protected override void OnStateEnter()
        {
            base.OnStateEnter();
            var targetEntity = _targetValueProvider.Get<Transform>();
            // var comp = _targetVarRef.Value;
            // if (comp == null)
            // {
            //     Debug.LogError("Target component is null. Cannot set parent.", this);
            //     return;
            // }
            var sourceTransform = _sourceValueProvider.Get<Transform>().transform;
            _oriParent = sourceTransform.parent; //記錄原本的parent
            var targetTransform = targetEntity?.transform;
            // _sourceValueProvider.Get<MonoEntity>().transform.SetParent(targetEntity.transform);
            if (sourceTransform == null)
            {
                Debug.LogError("Source transform is null. Cannot set parent.", this);
                return;
            }

            if (targetTransform == null)
            {
                // Debug.LogError("Target transform is null. Cannot set parent.", this);
                return;
            }

            sourceTransform.SetParent(targetTransform);
        }

        protected override void OnStateExit()
        {
            base.OnStateExit();
            var sourceTransform = _sourceValueProvider.Get<Transform>().transform;
            sourceTransform.SetParent(_oriParent);
        }

        // protected override void OnActionExecuteImplement()
        // {
        //
        // }

        [Button]
        public void SwitchSourceAndTarget()
        {
            //交換source和target
            (_sourceValueProvider, _targetValueProvider) = (
                _targetValueProvider,
                _sourceValueProvider
            );
        }
    }
}
