using System;
using MonoFSM.Condition;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    //FIXME: 用AbstractConditionActivateTarget

    //新規
    //可以直接放在該節點上
    //自動檢查條件，決定是否啟動節點
    //放在下面？
    [Obsolete]
    public class ConditionActivator : MonoBehaviour, IUIBehavior, ISelfValidator, IResetter, IConditionChangeListener
    {
        [Title("自動檢查條件，決定開關節點")]
        [CompRef]
        [AutoChildren]
        private AbstractConditionBehaviour[] conditions;

        [ReadOnly][ShowInPlayMode] private bool IsActivate => conditions.IsAllValid();

        //[]: 要有蠻多時間點的，updateView就要做？
        //[]: 操作後也需要，牽涉狀態改變？
        //[]: 這個節點上不可以放任何的condition node


        private bool HasConditionNodeOnThisNode => GetComponents<AbstractConditionBehaviour>().Length > 0;

        public void EnableCheck()
        {
            //FIXME: 可以是不同side effect類型嗎？
            //需要包一個proxy嗎？
            //check if gameObject is control by animation or state?
            if (IsActivate)
                // Debug.Log("IAdditionalChecker pass active true", gameObject);
                gameObject.SetActive(true);
            else
                // Debug.Log("IAdditionalChecker pass active false", gameObject);
                gameObject.SetActive(false);
        }

        public void Validate(SelfValidationResult result)
        {
            if (HasConditionNodeOnThisNode)
                result.AddError("把condition放在下面的節點，不要放在這個節點上");
        }

        public void EnterLevelReset() //不喜歡用這個名字
        {
            EnableCheck();
        }

        public void ExitLevelAndDestroy()
        {
        }

        //update check?
        // public void Update() //關起來就不會update了...
        // {
        //     EnableCheck();
        // }

        /// <summary>
        /// Condition改變時自動檢查條件
        /// </summary>
        public void OnConditionChanged()
        {
            EnableCheck();
        }
    }
}