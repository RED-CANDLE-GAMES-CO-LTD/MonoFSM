using System;
using MonoFSM.Runtime.Variable;
using MonoFSM.Runtime.Item_BuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    public interface IActor
    {
    }

    [Serializable] //沒用？
    public class GeneralEffectHitData : IEffectHitData
    {
        //反而是detector對detectable的資料？比較有用？
        public static GeneralEffectHitData Borrow(IEffectDealer dealer, IEffectReceiver receiver)
        {
            var data = new GeneralEffectHitData();
            data.Override(dealer, receiver);
            return data;
        }


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

        public void Override(IEffectDealer dealer, IEffectReceiver receiver)
        {
            _dealer = dealer as GeneralEffectDealer;
            _receiver = receiver as GeneralEffectReceiver;
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

        public Vector3 Dir =>
            hitPoint.HasValue && hitNormal.HasValue
                ? (hitPoint.Value - Receiver.transform.position).normalized
                : (Dealer.transform.position - Receiver.transform.position)
                .normalized;

        private Vector3? _hitPoint;
        private Vector3? _hitNormal;

        public T GetComponentFromDealerOwner<T>() where T : class
        {
            return GeneralDealer.GetComponentOfSibling<IModuleOwner, T>();
        }

        //從Owner身旁的MonoEntityBinder取得下面的Entity
        //FIXME: 不一定會有MonoEntityBinder? 還是要逼每個MonoObject都要有MonoEntityBinder? 那就應該要做成可以直接宣告的方式？(A has B)
        public T GetEntityFromDealerOwner<T>() where T : MonoEntity
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
        
        public T GetComponentFromReceiver<T>() where T : class
        {
            return GeneralReceiver.GetComponentOfSibling<IModuleOwner, T>();
        }
    }
}