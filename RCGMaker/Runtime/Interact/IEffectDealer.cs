using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core;
using UnityEngine;
using UnityEngine.Pool;


//dealer, effect hit
//interaction
//value 
// dealerA hit receiverB cause value
// damage = atk * damageRatio, 
//
public interface IEffectHitData
{
    IEffectDealer Dealer { get; }
    IEffectReceiver Receiver { get; }
    void Override(IEffectDealer dealer, IEffectReceiver receiver);
}

public interface IEffectType
{
}
public interface IEffectDealer
{
    // IEffectType getEffectType { get; }
    
    // void OnHitEnter(IEffectHitData data);
    // void OnHitStay(IEffectHitData data);
    // void OnHitExit(IEffectHitData data);
    bool CanHitReceiver(IEffectReceiver receiver);
}

public interface IEffectReceiver
{
    void EffectHitEnter(IEffectHitData data);
    // void OnHitStay(IEffectHitData data);
    // void OnHitExit(IEffectHitData data);
}

namespace RCGMaker.Core
{
    //假的
    public class TestEffectHitData : IEffectHitData
    {
        public IEffectDealer Dealer { get; private set; }
        public IEffectReceiver Receiver { get; private set; }

        public void Override(IEffectDealer dealer, IEffectReceiver receiver)
        {
            Dealer = dealer;
            Receiver = receiver;
        }

        private void Reset()
        {
            Dealer = null;
            Receiver = null;
        }

        public static ObjectPool<TestEffectHitData> hitDataPool = new(() => new TestEffectHitData(),
            data => data.Reset());
    }

//AddBuff直接把Dealer綁到Receiver上嗎？


//VirtualDealer? EffectDealer means no physics?
}