using System;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    public interface IEnableChecker
    {
        public void EnableCheck();
    }

    //新規
    //可以直接放在該節點上
    //自動檢查條件，決定是否啟動節點
    public class ConditionActivator : MonoBehaviour, IEnableChecker
    {
        [PreviewInInspector] [AutoChildren()] private AbstractConditionComp[] conditions;
        [ReadOnly] [ShowInPlayMode] private bool IsActivate => conditions.IsAllValid();

        //要有蠻多時間點的，updateView就要做？
        public void EnableCheck()
        {
            if (IsActivate)
            {
                Debug.Log("IAdditionalChecker pass active true", gameObject);
                gameObject.SetActive(true);
            }
            else
            {
                Debug.Log("IAdditionalChecker pass active false", gameObject);
                gameObject.SetActive(false);
            }
        }
    }
}