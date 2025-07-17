using System;
using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;


//dealer, effect hit
//interaction
//value 
// dealerA hit receiverB cause value
// damage = atk * damageRatio, 
//
// [Serializable]
// public class EffectHitEvent : UnityEvent<IEffectHitData>
// {
// }

public class CollisionHitData : IEffectHitData //fixme:
{
    public IEffectDealer Dealer { get; }
    public IEffectReceiver Receiver { get; }

    public void Override(IEffectDealer dealer, IEffectReceiver receiver)
    {
    }

    public Vector3? hitPoint { get; set; }
    public Vector3? hitNormal { get; set; }

    public Vector3 Dir => hitPoint.HasValue && hitNormal.HasValue
        ? (hitPoint.Value - Receiver.transform.position).normalized
        : Vector3.zero; //FIXME: 這個要不要放在這裡？還是放在EffectHitData裡面？
}

public interface IEffectHitData
{
    IEffectDealer Dealer { get; }
    IEffectReceiver Receiver { get; }
    void Override(IEffectDealer dealer, IEffectReceiver receiver);
    Vector3? hitPoint { get; set; } //FIXME: 這個要不要放在這裡？還是放在EffectHitData裡面？
    Vector3? hitNormal { get; set; } //FIXME: 這個要不要放在這裡？還是放在EffectHitData裡面？
    public Vector3 Dir { get; }
}

// public interface IEffectReceivedHandler //FIXME:和下面整和吧
// {
//     void OnEffectReceived(IEffectHitData data);
// }

public interface IEffectHitHandler
{
    public void EffectHitEnter(IEffectHitData data);
}

public interface IEffectReceivedProcessor
{
    void EffectHitResult(IEffectHitData hitData);
}

public interface IEffectType
{
}

public interface IEffectDealer //FIXME: 好像不需要interface
{
    // IEffectType getEffectType { get; }

    // void OnHitEnter(IEffectHitData data);
    // void OnHitStay(IEffectHitData data);
    // void OnHitExit(IEffectHitData data);
    // IEffectType getEffectType { get; }
    Transform transform { get; }
    bool CanHitReceiver(IEffectReceiver receiver);
    // float FinalValue { get; } //FIXME: 好像不該透過這個拿值？
}

public interface IEffectReceiver //FIXME: 好像不需要interface
{
    Transform transform { get; }

    void OnEffectHitEnter(IEffectHitData data);

    // void OnHitStay(IEffectHitData data);
    void OnEffectHitExit(IEffectHitData data);

    // float ReactValue { get; }
    bool IsValid { get; }
}

namespace MonoFSM.Core
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

        public Vector3? hitPoint { get; set; }

        public Vector3? hitNormal { get; set; }

        public Vector3 Dir => hitPoint.HasValue && hitNormal.HasValue
            ? (hitPoint.Value - Receiver.transform.position).normalized
            : Vector3.zero; //FIXME: 這個要不要放在這裡？還是放在EffectHitData裡面？


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