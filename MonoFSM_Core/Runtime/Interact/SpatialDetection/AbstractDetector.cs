using System;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core.Detection
{
    [DisallowMultipleComponent]
    public abstract class AbstractDetector : MonoBehaviour, IDefaultSerializable
    {
        [PreviewInInspector] [Component] [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionComp[] _conditions;

        public bool IsValid => _conditions.IsAllValid();

        private List<EffectDetectable> toRemove = new();

        //FIXME: Receiver的部分要怎麼處理？ 也會有開關的問題？還是沒差遇到再說
        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            // Debug.Log("OnDisable of detector",this);
            //copy _detectedObjects to toRemove
            toRemove.AddRange(_detectedObjects);
            foreach (var detectable in toRemove)
                // Debug.Log("OnDisable of detectable",detectable);
                OnSpatialExit(detectable.gameObject);

            toRemove.Clear();
        }

        [AutoParent] private StateMachineOwner owner;
        public StateMachineOwner Owner => owner;
        [PreviewInInspector] [AutoChildren] private GeneralEffectDealer[] dealers;

        //GameObject必定要在Detector的layer
        [FormerlySerializedAs("hittingLayer")]
        [CustomSerializable]
        [ShowInInspector]
        [OnValueChanged(nameof(SetLayerOverride))]
        public LayerMask HittingLayer;

        //FIXME: 這個要做什麼？
        // [Button]
        // void SerializeTest()
        // {
        //     //太廢了吧...
        //     var result = JsonUtility.ToJson(HittingLayer);
        //     Debug.Log(result);
        // }

        protected abstract void SetLayerOverride();

        [PreviewInInspector] protected List<EffectDetectable> _detectedObjects = new();
#if UNITY_EDITOR
        [PreviewInInspector] protected List<EffectDetectable> _lastDetectedObjects = new();

        [Button]
        private void ClearLastDetectedObjects()
        {
            _lastDetectedObjects.Clear();
        }
#endif

        //FIXME: 這個是spatial Detector的特性，不是所有的Detector都有
        public void OnSpatialEnter(GameObject other) //可能需要帶其他額外參數？像是collision的資訊
        {
            if (IsValid == false) //條件不符合
                return;
            //理論上不該打到別的東西，layer就擋掉了才對 (有分layer的話)
            if (!other.TryGetComponent<EffectDetectable>(out var spatialDetectable))
            {
                Debug.LogError(other.name + " is not a EffectDetectable" + other.gameObject.layer, other);

                return;
            }

            //FIXME: 物理的想要繞掉，另外做condition?
            // if (spatialDetectable.Owner == Owner) return; //自己身上的不算
            _detectedObjects.Add(spatialDetectable);
#if UNITY_EDITOR
            _lastDetectedObjects.Add(spatialDetectable);
#endif
            spatialDetectable._detectors.Add(this);
            // Debug.Log("OnSpatialEnter dealers:"+dealers.Length+" receivers:"+effectCollider.EffectReceivers.Length, this);
            //FIXME: 用update撈起來等等再判？
            foreach (var dealer in dealers)
            {
                if (!dealer.IsValid) continue;

                foreach (var receiver in spatialDetectable.EffectReceivers)
                {
                    //FIXME: proxy的判定
                    if (!dealer.CanHitReceiver(receiver)) continue; //不會打到的不算
                    //移到System?
                    //互動雙方的條件描述
                    var hitData = receiver.GenerateEffectHitData(dealer, receiver);
                    dealer.OnHitEnter(hitData);
                    receiver.OnEffectHitEnter(hitData);
                }
            }
        }

        public void OnSpatialExit(GameObject other)
        {
            if (!other.TryGetComponent<EffectDetectable>(out var spatialDetectable))
                // Debug.LogError(other.name + " is not a GeneralEffectCollider" + other.gameObject.layer);
                return;

            _detectedObjects.Remove(spatialDetectable);
            spatialDetectable._detectors.Remove(this);
            //FIXME: 連點會有狀態問題耶...
            foreach (var dealer in dealers)
                //FIXME: 點下去，可能就造成dealer的condition變了耶
                // if (!dealer.IsValid) //有點討厭，這個很容易漏掉, 這個會讓
                // {
                //     dealer.Log("Dealer is not valid");
                //     continue;
                // }
                //Dealer觸發後，造成條件變化了，這樣這邊會很難判定？
            foreach (var receiver in spatialDetectable.EffectReceivers)
            {
                //對稱
                if (!dealer.IsEnteredReceiver(receiver)) continue;
                // if (!dealer.CanHitReceiver(receiver)) continue;
                //FIXME: 這個是不是不該generate? 還是重新gen也還好
                var hitData = receiver.GenerateEffectHitData(dealer, receiver);

                dealer.OnHitExit(hitData);
                receiver.OnEffectHitExit(hitData);
            }
        }
    }
}