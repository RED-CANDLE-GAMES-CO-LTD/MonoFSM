using System;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _3_Script._0_RedCandleGamesUtilities.UICanvas.ActivateChecker
{
    //這個要整個Panel OnEnable的時候才會檢查一遍，不會隨時檢查
    //ActivateChecker
    public class AbstractConditionActivateTarget : MonoBehaviour, ISubmitHandler//, ISelectedInstanceUpdater
    {
        //FIXME: 這邊的UI想要看有沒有被檢查
#if UNITY_EDITOR //這只是想拿來看的...繞掉的attribute? [NonCache] ?
         [Required] [PreviewInInspector] [AutoParent]
         private ConditionActivateCheckProvider parentConditionActivateCheckProvider;
//
//         [Title("有沒有在Update Loop檢查Condition")]
//         [PreviewInInspector]
//         private bool isCheckResultAtUpdateLoop => parentConditionActivateCheckProvider?.IsUpdate ?? false;
#endif
        //這個是不是太多層了...
        [Component] //沒用...
        [AutoChildren]
        [ShowInInspector]
        private AbstractConditionComp[] conditions = Array.Empty<AbstractConditionComp>();

        [PreviewInInspector] protected virtual bool result => conditions.IsAllValid();

        public virtual void ActivateCheck()
        {
            gameObject.SetActive(result);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            ActivateCheck();
        }

        public void UpdateView(IDescriptable data) //更新所選的instance時檢查看看
        {
            ActivateCheck();
        }
    }
}