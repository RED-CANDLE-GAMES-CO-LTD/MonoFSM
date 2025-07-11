using System;
using _3_Script._0_RedCandleGamesUtilities.UICanvas.ActivateChecker;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonoFSM.Core.Condition
{
    //這個要整個Panel OnEnable的時候才會檢查一遍，不會隨時檢查
    //ActivateChecker
    public abstract class //IReturnToPool? IDespawn?
        AbstractConditionActivateTarget : MonoBehaviour, IUpdateSimulate //, ISelectedInstanceUpdater //ISubmitHandler
    {

        public void Simulate(float deltaTime)
        {
            //FIXME: Input Condition觸發不了？
            ActivateCheck();
        }

        public void AfterUpdate()
        {
        }
        //這個是不是太多層了...
        [Component] //沒用...
        [AutoChildren(DepthOneOnly = true)]
        [ShowInInspector]
        private AbstractConditionBehaviour[] _conditions = Array.Empty<AbstractConditionBehaviour>();

        [PreviewInInspector] protected virtual bool result => _conditions.IsAllValid();

        public abstract void ActivateCheck();

        // public void OnSubmit(BaseEventData eventData)
        // {
        //     ActivateCheck();
        // }
    }
}