using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RCGMakerFSMCore.Editor.ViewWindow.HierarchyTab
{
    public class FindToolbarSearchField:ToolbarSearchField
    {
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);
            // Debug.Log("HandleEventBubbleUp");
            // evt.StopPropagation();
        }

        protected override void HandleEventTrickleDown(EventBase evt)
        {
            base.HandleEventTrickleDown(evt);
            // Debug.Log("HandleTrickleDown");
        }
    }
}