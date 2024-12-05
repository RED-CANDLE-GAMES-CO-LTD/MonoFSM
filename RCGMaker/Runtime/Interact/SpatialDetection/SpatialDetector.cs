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
    public abstract class SpatialDetector : MonoBehaviour, IDefaultSerializable
    {
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

        [PreviewInInspector]
        protected List<SpatialDetectable> _detectedObjects = new List<SpatialDetectable>();
        public void OnSpatialEnter(GameObject other) //可能需要帶其他額外參數？像是collision的資訊
        {
            //理論上不該打到別的東西，layer就擋掉了才對 (有分layer的話)
            if (!other.TryGetComponent<SpatialDetectable>(out var effectCollider))
            {
                // Debug.LogError(other.name + " is not a GeneralEffectCollider" + other.gameObject.layer, other);
                return;
            }
            _detectedObjects.Add(effectCollider);
            // Debug.Log("OnSpatialEnter dealers:"+dealers.Length+" receivers:"+effectCollider.EffectReceivers.Length, this);
            //FIXME: 用update撈起來等等再判？
            foreach (var dealer in dealers)
            {
                foreach (var receiver in effectCollider.EffectReceivers)
                {
                    if (!dealer.CanHitReceiver(receiver)) continue;
                    var hitData = receiver.GenerateEffectHitData(dealer, receiver);
                    dealer.OnHitEnter(hitData);
                    receiver.OnEffectHitEnter(hitData);
                }
            }
        }

        public void OnSpatialExit(GameObject other)
        {
            if (!other.TryGetComponent<SpatialDetectable>(out var effectCollider))
            {
                // Debug.LogError(other.name + " is not a GeneralEffectCollider" + other.gameObject.layer);
                return;
            }
            _detectedObjects.Remove(effectCollider);
            foreach (var dealer in dealers)
            {
                foreach (var receiver in effectCollider.EffectReceivers)
                {
                    if (!dealer.CanHitReceiver(receiver)) continue;
                    var hitData = receiver.GenerateEffectHitData(dealer, receiver);
                    dealer.OnHitExit(hitData);
                    receiver.OnEffectHitExit(hitData);
                }
            }
        }
    }
}