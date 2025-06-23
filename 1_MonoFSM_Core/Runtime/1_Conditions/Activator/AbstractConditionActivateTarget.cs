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
      
        //FIXME: 這邊的UI想要看有沒有被檢查
// #if UNITY_EDITOR //這只是想拿來看的...繞掉的attribute? [NonCache] ?
        // [Required] [PreviewInInspector] [AutoParent]
        // private ConditionActivateCheckProvider parentConditionActivateCheckProvider;
//
//         [Title("有沒有在Update Loop檢查Condition")]
//         [PreviewInInspector]
//         private bool isCheckResultAtUpdateLoop => parentConditionActivateCheckProvider?.IsUpdate ?? false;
// #endif

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
        private AbstractConditionComp[] _conditions = Array.Empty<AbstractConditionComp>();

        [PreviewInInspector] protected virtual bool result => _conditions.IsAllValid();

        public abstract void ActivateCheck();

        // public void OnSubmit(BaseEventData eventData)
        // {
        //     ActivateCheck();
        // }
        //
        // public void UpdateView(IDescriptableData data) //更新所選的instance時檢查看看
        // {
        //     ActivateCheck();
        // }
    }
}