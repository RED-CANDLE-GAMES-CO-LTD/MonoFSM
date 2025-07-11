using System;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;


namespace MonoFSM.Core
{
    //舊規，開Child
    //掛在上面，會被上面檢查？
    [Obsolete]
    public class ConditionNodeActivator : MonoBehaviour, IResetter
    {
        // // [Header("是否反向表達")] public bool IsInverted;
        // [InlineEditor] public PlayerAbilityData abilityData;
        [PreviewInInspector] [AutoChildren()] private AbstractConditionBehaviour[] conditions;

        // [AutoChildren(false)] private IAdditionalChecker[] _additionalCheckers;
        [ReadOnly] [ShowInPlayMode] public bool IsActivate => conditions.IsAllValid();

        public GameObject childNode;

        private void OnValidate()
        {
            if (childNode == null)
                childNode = transform.GetChild(0).gameObject;
        }

        private void ActivateNode(bool value)
        {
            //FIXME: 這個進入點怪怪的
            childNode.SetActive(value);
        }

        private void Update() //需要update檢查嗎？
        {
            var result = IsActivate;
            if (result && childNode.activeSelf == false)
            {
                Debug.Log("Set Child Activate true", gameObject);
                childNode.SetActive(true);
            }

            else if (result == false && childNode.activeSelf)
            {
                Debug.Log("Set Child Activate false", gameObject);
                childNode.SetActive(false);
            }
        }

        //Checker要一直開著，條件不對的時候，關掉下方的節點
        // private void OnEnable()
        // {
        //     // Debug.Log("IAdditionalChecker pass active true", gameObject);
        //     // childNode.SetActive(IsActivate);
        // }

        public void Init()
        {
            ActivateNode(IsActivate);
            // if (abilityData.IsActivated == false) //還沒開，才需要註冊
            //還是有可能拔掉？？？
            // abilityData.OnActivate.AddListener(ActivateNode);
        }
        //TODO: Player要往下找？做檢查？
        //  void ActivateCheck()
        // {
        //     Debug.Log("Ability ActivateCheck:" + abilityData.IsActivated + abilityData.name, gameObject);
        //
        //     ActivateNode(abilityData.IsActivated);
        // }

        public void EnterLevelReset()
        {
            // throw new System.NotImplementedException();
            Init();
        }

        public void ExitLevelAndDestroy()
        {
        }

        // public void EnableCheck()
        // {
        //     if (conditions.IsAllValid())
        //     {
        //         gameObject.SetActive(true);
        //     }
        //     else
        //     {
        //         gameObject.SetActive(false);
        //     }
        // }
    }
}