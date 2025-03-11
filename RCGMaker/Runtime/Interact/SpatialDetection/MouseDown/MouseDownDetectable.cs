using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.Interact.EffectHit;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.SpatialDetection
{
    public class MouseDownDetectable : SpatialDetectable
    {
        //
        private void OnMouseOver()
        {
            // Debug.Log("OnMouseOver", this);
        }

        private void OnMouseDown()
        {
            if (!_conditions.IsAllValid())
            {
                Debug.Log("Conditions not met", this);
                return;
            }

            //current mouse effectDealer?
            var detector = MouseDownDetector.Instance;
            // if(detector.)
            Debug.Log("OnMouseDown", this);
            detector.OnSpatialEnter(gameObject);
            //TODO: 馬上就Exit?
            //FIXME: 連點會有狀態問題耶...
            detector.OnSpatialExit(gameObject);
            // foreach (var effectReceiver in EffectReceivers)
            // {
            //     effectReceiver.OnEffectHit();
            // }
        }

        [Component] [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        AbstractConditionComp[] _conditions = Array.Empty<AbstractConditionComp>();
    }
}