using System;
using MonoFSM.Core.EffectHit;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    public interface IActor { }

    //TODO: 要用Struct還是用 Class? 好像是為了要pass下去
    [Serializable] //沒用？
    public class GeneralEffectHitData : IEffectHitData
    {
        //反而是detector對detectable的資料？比較有用？
        //FIXME: 需要實作borrow?
        // public static GeneralEffectHitData Borrow(IEffectDealer dealer, IEffectReceiver receiver)
        // {
        //     var data = new GeneralEffectHitData();
        //     data.Override(dealer, receiver);
        //     return data;
        // }

        //這裡應該包含Rigidbody?
        //dealer和receiver的transform資料可以作為相對位置
        [ShowInInspector]
        public IEffectDealer Dealer => _dealer;

        [ShowInInspector]
        public IEffectReceiver Receiver => _receiver;

        public IActor Source => _dealer.Owner;
        public IActor Target => _receiver.Owner;

        public GeneralEffectDealer GeneralDealer => _dealer;
        public GeneralEffectReceiver GeneralReceiver => _receiver;

        private GeneralEffectDealer _dealer;
        private GeneralEffectReceiver _receiver;

        public BaseEffectDetectTarget _receiverSourceObj;

        public void Override(
            IEffectDealer dealer,
            IEffectReceiver receiver,
            BaseEffectDetectTarget receiverSourceObj
        )
        {
            _dealer = dealer as GeneralEffectDealer;
            _receiver = receiver as GeneralEffectReceiver;
            _receiverSourceObj = receiverSourceObj;
            hitPoint = null; //重置hitPoint
            hitNormal = null; //重置hitNormal
        }


        [ShowInInspector]
        public Vector3? hitPoint
        {
            get => _hitPoint;
            set => _hitPoint = value;
        }

        [ShowInInspector]
        public Vector3? hitNormal
        {
            get => _hitNormal;
            set => _hitNormal = value;
        }

        [ShowInInspector]
        public Vector3? hitDirection
        {
            get => _hitDirection;
            set => _hitDirection = value;
        }

        //合理的設計嗎？force direction? 和normal無關，從dealer推測力的方向
        //TODO: 好像還要包含dealer的rotation?
        public Vector3 Dir => hitDirection ??
                              (Receiver.transform.position - Dealer.transform.position).normalized;
        // (
        //     hitPoint.HasValue && hitNormal.HasValue
        //         ? (hitPoint.Value - Dealer.transform.position).normalized
        //         : (Receiver.transform.position - Dealer.transform.position).normalized));

        private Vector3? _hitPoint;
        private Vector3? _hitNormal;
        private Vector3? _hitDirection;

        public T GetComponentFromDealerOwner<T>()
            where T : class
        {
            return GeneralDealer.GetComponentOfSibling<IModuleOwner, T>();
        }

        //從Owner身旁的MonoEntityBinder取得下面的Entity
        //FIXME: 不一定會有MonoEntityBinder? 還是要逼每個MonoObject都要有MonoEntityBinder? 那就應該要做成可以直接宣告的方式？(A has B)
        public T GetEntityFromDealerOwner<T>()
            where T : MonoEntity
        {
            //FIXME: 現在根本兩個就一樣..
            var binder = GeneralDealer.GetComponentInParent<MonoEntityBinder>();
            // var binder = GeneralDealer.GetComponentOfSibling<IModuleOwner, MonoEntityBinder>();
            if (binder == null)
            {
                Debug.LogError("No MonoEntityBinder found in Dealer Owner", GeneralDealer);
                return null;
            }

            Debug.Log("GetEntityFromDealerOwner " + typeof(T).Name, binder);
            return binder.Get(typeof(T)) as T; //有點醜
        }

        public T GetComponentFromReceiver<T>()
            where T : class
        {
            return GeneralReceiver.GetComponentOfSibling<IModuleOwner, T>();
        }
    }
}
