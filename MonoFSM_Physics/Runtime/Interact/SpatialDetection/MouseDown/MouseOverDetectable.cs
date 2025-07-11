using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.SpatialDetection
{
    public class MouseOverDetectable : EffectDetectable
    {
        [Component] [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        private AbstractConditionBehaviour[] _conditions = Array.Empty<AbstractConditionBehaviour>();

        public void OnMouseEnter()
        {
            // Debug.Log("OnMouseEnter", this);
            //可以顯示UI那類的
            if (!_conditions.IsAllValid())
            {
                Debug.Log("MouseDownDetectable Conditions not met", this);
                return;
            }

            //current mouse effectDealer?
            var detector = MouseDetector.Instance;
            // if(detector.)
            // Debug.Log("OnMouseDown", this);
            detector.OnDetectEnter(gameObject);
            //TODO: 馬上就Exit?
            //FIXME: 連點會有狀態問題耶...
            //FIXME: 要條件對才可以做這件事？
        }

        public void OnMouseExit()
        {
            var detector = MouseDetector.Instance;
            detector.OnDetectExit(gameObject);
            // foreach (var effectReceiver in EffectReceivers)
            // {
            //     effectReceiver.OnEffectHit();
            // }
        }
    }
}