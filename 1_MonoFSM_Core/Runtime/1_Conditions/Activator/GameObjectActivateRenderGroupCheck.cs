using System;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions.Activator
{
    public interface IActivateCheckTarget
    {
        public bool IsValid { get; }
        public GameObject gameObject { get; }
    }

    /// <summary>
    ///     每個 LateUpdate 掃子孫的 IActivateCheckTarget（例如 TextValueBinder），依它們的 IsValid
    ///     SetActive 開關該物件；UI 上「這一行要不要顯示」就靠它。
    ///     掛在一個純容器節點上，底下放要被開關的 binder —— binder 自己身上的 condition 只有這支會讀，
    ///     parent 鏈上沒有它的話那些 condition 等於白寫。
    ///     它繼承 AbstractDescriptionBehaviour，存檔時節點名會被蓋成 GameObjectActivateRenderGroupCheck，
    ///     所以不要掛在有語意名稱的節點上。
    /// </summary>
    public class GameObjectActivateRenderGroupCheck : AbstractDescriptionBehaviour
    {
        [ShowInInspector] [AutoChildren] IActivateCheckTarget[] _targets;

        private void LateUpdate()
        {
            if (_targets == null)
            {
                Debug.LogError("null targets", this);
                return;
            }
            foreach (var target in _targets)
            {
                if (target.gameObject.activeSelf != target.IsValid)
                    target.gameObject.SetActive(target.IsValid);
            }
        }
    }
}
