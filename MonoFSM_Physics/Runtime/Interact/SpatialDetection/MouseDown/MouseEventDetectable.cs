using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Interact.SpatialDetection
{
    public class MouseEventDetectable : MonoBehaviour
    {
        //dispatch to children?
        [AutoChildren] public MouseDownDetectable _mouseDownDetectable;

        [AutoChildren] public MouseOverDetectable _detectable;

        private void OnMouseEnter()
        {
            if (!_conditions.IsAllValid())
            {
                Debug.Log("MouseDownDetectable Conditions not met", this);
                return;
            }

            _detectable.OnMouseEnter();
        }

        private void OnMouseExit()
        {
            _detectable.OnMouseExit();
        }

        private void OnMouseDown()
        {
            if (!_conditions.IsAllValid())
            {
                Debug.Log("MouseDownDetectable Conditions not met", this);
                return;
            }

            //current mouse effectDealer?
            var detector = MouseDetector.Instance;
            // if(detector.)
            // Debug.Log("OnMouseDown", this);
            detector.OnDetectEnter(_mouseDownDetectable.gameObject);
            //TODO: 馬上就Exit?
            //FIXME: 連點會有狀態問題耶...
            //FIXME: 要條件對才可以做這件事？
            detector.OnDetectExit(_mouseDownDetectable.gameObject);
            _mouseDownDetectable.HandleMouseDown(detector);
            // foreach (var effectReceiver in EffectReceivers)
            // {
            //     effectReceiver.OnEffectHit();
            // }
        }

        [Component] [AutoChildren(DepthOneOnly = true)] [PreviewInInspector]
        //條件要下放嗎？
        private AbstractConditionBehaviour[] _conditions = Array.Empty<AbstractConditionBehaviour>();
    }
}