using System;
using RCGMaker.Runtime.Interact.EffectHit;

namespace RCGMaker.Runtime.Interact.SpatialDetection
{
    public class MouseDownDetectable:SpatialDetectable 
    {
        //
        private void OnMouseDown()
        {
            //current mouse effectDealer?
            MouseDownDetector.Instance.OnSpatialEnter(gameObject);
            // foreach (var effectReceiver in EffectReceivers)
            // {
            //     effectReceiver.OnEffectHit();
            // }
        }
    }
}