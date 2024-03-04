using System;
using RCGMaker.Runtime.Interact.EffectHit;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public abstract class SpatialDetector : MonoBehaviour
    {
        [AutoChildren] private IEffectDealer[] dealers;

        protected virtual void OnSpatialEnter(Transform other)
        {
            //FIXME: 用cache來拿
            //還是應該也要做個接口給receiver比較好
            var receivers = other.GetComponentsInChildren<IEffectReceiver>();
            foreach (var dealer in dealers)
            {
                foreach (var receiver in receivers)
                {
                    if (dealer.CanHitReceiver(receiver))
                    {
                        //TODO: 打下去..
                    }
                }
            }
        }
    }
}