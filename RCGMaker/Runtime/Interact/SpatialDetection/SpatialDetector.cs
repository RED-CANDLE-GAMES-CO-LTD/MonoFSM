using System;
using RCGMaker.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core.Detection
{
    
    public abstract class SpatialDetector : MonoBehaviour, IDefaultSerializable
    {
        [ShowInInspector] [AutoChildren] private GeneralEffectDealer[] dealers;

        //GameObject必定要在Detector的layer
        [FormerlySerializedAs("hittingLayer")]
        [CustomSerializable]
        [ShowInInspector]
        [OnValueChanged(nameof(SetLayerOverride))]
        public LayerMask HittingLayer;

        [Button]
        void SerializeTest()
        {
            //太廢了吧...
            var result = JsonUtility.ToJson(HittingLayer);
            Debug.Log(result);
        }

        protected abstract void SetLayerOverride();

        protected void OnSpatialEnter(GameObject other) //可能需要帶其他額外參數？像是collision的資訊
        {
            //理論上不該打到別的東西，layer就擋掉了才對
            if (!other.TryGetComponent<SpatialDetectable>(out var effectCollider))
            {
                Debug.LogError(other.name + " is not a GeneralEffectCollider" + other.gameObject.layer, this);
                return;
            }

            //FIXME: 用update撈起來等等再判？
            foreach (var dealer in dealers)
            {
                foreach (var receiver in effectCollider.EffectReceivers)
                {
                    if (!dealer.CanHitReceiver(receiver)) continue;
                    var hitData = receiver.GenerateEffectHitData(dealer, receiver);
                    dealer.OnHitEnter(hitData);
                    receiver.EffectHitEnter(hitData);
                }
            }
        }

        protected void OnSpatialExit(GameObject other)
        {
            if (!other.TryGetComponent<SpatialDetectable>(out var effectCollider))
            {
                Debug.LogError(other.name + " is not a GeneralEffectCollider" + other.gameObject.layer);
                return;
            }

            foreach (var dealer in dealers)
            {
                foreach (var receiver in effectCollider.EffectReceivers)
                {
                    if (!dealer.CanHitReceiver(receiver)) continue;
                    var hitData = receiver.GenerateEffectHitData(dealer, receiver);
                    dealer.OnHitExit(hitData);
                    receiver.OnHitExit(hitData);
                }
            }
        }
    }
}