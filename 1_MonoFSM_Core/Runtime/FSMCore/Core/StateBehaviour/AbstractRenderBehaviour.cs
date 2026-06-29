using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    public abstract class AbstractRenderBehaviour : AbstractDescriptionBehaviour, IRenderBehaiour
    {
        [ShowInInspector] [Required] [AutoParent]
        IRenderInvoker _iRenderInvoker;

        [AutoNested]
        [InlineField]
        [PropertyOrder(1)]
        public ConditionGroup _conditionGroup; //condition 成立，才能觸發 Render

        //比照 AbstractStateAction.IsValid，condition group 不成立就不觸發
        protected bool IsConditionValid => _conditionGroup.IsValid;

        protected override bool HasError()
        {
            return _iRenderInvoker == null || base.HasError();
        }

        protected override string DescriptionTag => "Render";
        [ShowInDebugMode] private float _lastRenderTime;
        [ShowInDebugMode] private float _lastEnterRenderTime;
        public abstract void OnEnterRenderImplement();

        public virtual void OnRenderImplement()
        {
            //沒override就當作一樣的事
            //FIXME: 這個好嗎？會被狂call?
            // OnEnterRenderImplement();
        }

        public void OnEnterRender()
        {
            if (isActiveAndEnabled == false)
                return;
            if (IsConditionValid == false)
                return;
            _lastEnterRenderTime = Time.time;
            OnEnterRenderImplement();
        }


        public void OnRender()
        {
            if (isActiveAndEnabled == false)
                return;
            if (IsConditionValid == false)
                return;
            _lastRenderTime = Time.time;
            OnRenderImplement();
        }
    }
}
