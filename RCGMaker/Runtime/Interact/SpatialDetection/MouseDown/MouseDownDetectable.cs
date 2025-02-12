using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.Interact.EffectHit;

namespace RCGMaker.Runtime.Interact.SpatialDetection
{
    public class MouseDownDetectable : SpatialDetectable
    {
        //
        private void OnMouseDown()
        {
            if (!_conditions.IsAllValid())
                return;
            //current mouse effectDealer?
            var detector = MouseDownDetector.Instance;
            // if(detector.)
            detector.OnSpatialEnter(gameObject);
            //TODO: 馬上就Exit?
            //FIXME: 連點會有狀態問題耶...
            detector.OnSpatialExit(gameObject);
            // foreach (var effectReceiver in EffectReceivers)
            // {
            //     effectReceiver.OnEffectHit();
            // }
        }

        [Component] [AutoChildren] [PreviewInInspector]
        AbstractConditionComp[] _conditions = Array.Empty<AbstractConditionComp>();
    }
}