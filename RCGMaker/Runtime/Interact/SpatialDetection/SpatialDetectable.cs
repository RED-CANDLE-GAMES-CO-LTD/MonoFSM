using System.Collections.Generic;
using RCGMaker.Core.Detection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    [DisallowMultipleComponent]
    //空間中的物件，可以被偵測到, 基本上會有collider或是collider2D
    //從Detector過來
    public class SpatialDetectable : MonoBehaviour, IDefaultSerializable //mousedown?
    {
        [AutoParent] private StateMachineOwner owner;

        public StateMachineOwner Owner => owner;

        //FIXME: 確保layer有設定
        [Component] [AutoChildren] GeneralEffectReceiver[] _effectReceivers;
        [ShowInInspector] public GeneralEffectReceiver[] EffectReceivers => _effectReceivers;
        [AutoParent] private Collider _collider;
        public Collider MyCollider => _collider;

        //DebugOnly
        public List<AbstractDetector> _detectors;


        // List<SpatialDetector> fromDetectors;
        private List<AbstractDetector> toRemoves = new List<AbstractDetector>();

        private void OnDisable()
        {
            //FIXME: 標記狀態改變，不要在這裡執行OnSpatialExit?
            if (!Application.isPlaying)
                return;
            toRemoves.AddRange(_detectors);
            foreach (var toRemove in toRemoves)
            {
                Debug.Log("OnDisable of Detectable", this);
                Debug.Log("OnDisable of Detectable removef from" + toRemove, toRemove);
                toRemove.OnSpatialExit(gameObject);

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