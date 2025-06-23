using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Detection;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    [DisallowMultipleComponent]
    //空間中的物件，可以被偵測到, 基本上會有collider或是collider2D
    //從Detector過來
    public class EffectDetectable : MonoBehaviour, IDefaultSerializable //mousedown?
    {
        [AutoParent] private StateMachineOwner owner;

        public StateMachineOwner Owner => owner;

        //FIXME: 確保layer有設定
        [Component] [AutoChildren(DepthOneOnly = true)]
        private GeneralEffectReceiver[] _effectReceivers;

        [ShowInInspector] public GeneralEffectReceiver[] EffectReceivers => _effectReceivers;
        [AutoParent] private Collider _collider;
        public Collider MyCollider => _collider;

        //DebugOnly
        #if UNITY_EDITOR
        [PreviewInInspector]
        public HashSet<AbstractDetector> _detectors = new();
        #endif


        // List<SpatialDetector> fromDetectors;
        [PreviewInInspector]
        private HashSet<AbstractDetector> toRemoves = new();

        private void OnDisable()
        {
            //FIXME: 標記狀態改變，不要在這裡執行OnSpatialExit?
            if (!Application.isPlaying)
                return;
#if UNITY_EDITOR
            toRemoves.AddRange(_detectors);
            #endif
            foreach (var toRemove in toRemoves)
            {
                // Debug.Log("OnDisable of Detectable", this);
                // Debug.Log("OnDisable of Detectable removef from" + toRemove, toRemove);
                toRemove.OnDetectExit(gameObject);

                //copy _detectedObjects to toRemove
                // toRemove.AddRange(_detectedObjects);
                // foreach (var detectable in toRemove)
                // {
                //     // Debug.Log("OnDisable of detectable",detectable);
                //     OnTriggerExit(detectable.MyCollider);
                // }
                // toRemove.Clear();
            }

            toRemoves.Clear();
        }
    }
}