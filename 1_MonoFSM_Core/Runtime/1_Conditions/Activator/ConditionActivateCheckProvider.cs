using MonoFSM.Core.Attributes;
using MonoFSM.Core.Condition;
using UnityEngine;

namespace _3_Script._0_RedCandleGamesUtilities.UICanvas.ActivateChecker
{
    //掛在UIControlGroup (或是掛在物件的root)
    //會在OnPanelShow,OnEnable,(Update)時檢查 下面的 AbstractConditionActivateTarget
    //FIXME: UI變化時可以監聽
    public class ConditionActivateCheckProvider : MonoBehaviour
    {
        [PreviewInInspector] [AutoChildren] private AbstractConditionActivateTarget[] conditionActivateTargets;

        //FIXME: 這個應該可以抽象掉？
        // [PreviewInInspector] [Auto(false)] private UIControlGroup uiControlGroup;

        private void Update() //希望可以不要update?
        {
            if (IsUpdate) //需要比較細的判定可以把這個勾起來
                Check();
        }

        public bool IsUpdate = false;

        private void Check()
        {
            if (gameObject.activeSelf == false)
                return;
            // Debug.Log("ActivateCheck: Check", this);
            foreach (var conditionActivateTarget in conditionActivateTargets)
            {
                // Debug.Log("ActivateCheck: target" + conditionActivateTarget, conditionActivateTarget);
                conditionActivateTarget.ActivateCheck();
            }
        }

        private void OnEnable()
        {
            Check();
        }
    }
}