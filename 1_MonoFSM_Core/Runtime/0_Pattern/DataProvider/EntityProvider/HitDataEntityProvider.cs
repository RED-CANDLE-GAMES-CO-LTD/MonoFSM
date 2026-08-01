using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime;
using MonoFSM.Runtime;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;
using UnityEngine.Serialization;

[Obsolete("不可信任XDD 還是直接從arg event當下拿比較好，還是可以修回來？")] //FIXME: 不可信任XDD 還是直接從arg event當下拿比較好
public class
    HitDataEntityProvider : AbstractEntitySource,
    IEntityValueProvider //這個介面很怪？VariableOwner...那就直接I
{
    //可是這裡
    [CompRef]
    [AutoParent]
    private IHitDataProvider _hitDataProvider;

    public enum HitDataVariableOwner
    {
        Dealer, //rename?
        Receiver,
    }

    //FIXME: Owner可以 自動判斷吧，parent有Dealer就表示要用Receiver的
    // string IEntityValueProvider.Description => $"{ownerType}'s Entity";

    public HitDataVariableOwner ownerType;

    public override MonoEntity monoEntity //runtime才會有，要有？
    {
        get
        {
            if (Application.isPlaying == false)
                return null;
            if (_hitDataProvider == null)
            {
                Debug.LogError("HitDataProvider is null in HitDataVariableOwnerProvider", this);
                return null;
            }

            var hitData = _hitDataProvider.GetGeneralHitData();
            if (hitData == null)
                // Debug.LogError("HitData is null in HitDataVariableOwnerProvider", this);
                return null;
            switch (ownerType)
            {
                case HitDataVariableOwner.Dealer:
                    // Debug.Log(" HitDataVariableOwner.DealerOwner", hitData.Dealer.transform);
                    return hitData.GeneralDealer.BindEntity;
                case HitDataVariableOwner.Receiver:
                    // Debug.Log(" HitDataVariableOwner.ReceiverOwner", hitData.Receiver.transform);
                    return hitData.GeneralReceiver.BindEntity;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public override string SuggestDeclarationName => "HitData";

    // public override string NickName => "HitData";


    [ShowInDebugMode]
    private IEffectHitData currentHitData => _hitDataProvider?.GetHitData();
}

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    /// 從HitDataProvider，從hitData 來拿到 Dealer/Receiver 的 Parent Component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class HitDataParentCompProvider<T> : MonoBehaviour
    {
        [Required]
        [CompRef]
        [AutoParent]
        private IHitDataProvider _hitDataProvider;

        [FormerlySerializedAs("ownerType")]
        [FormerlySerializedAs("_targetType")]
        public EffectHitTarget _targetType;

        // #if UNITY_EDITOR
        [PreviewInInspector]
        private IEffectHitData HitData => _hitDataProvider.GetHitData();

        // #endif

        // private T _cached;

        //FIXME: 效能不好 事先cache?
        //last hitData same才可以耶？dealer/receiver dictionary?
        protected T GetParentComp()
        {
            if (Application.isPlaying == false)
                return default;
            var hitData = HitData;
            if (hitData == null)
                // Debug.LogError("HitData is null");
                return default;
            // if (_cached != null)
            //     return _cached;

            //第一次
            switch (_targetType)
            {
                case EffectHitTarget.Dealer:

                    // Debug.Log(" HitDataVariableOwner.DealerOwner", hitData.Dealer.transform);
                    return _hitDataProvider.GetHitData().Dealer.transform.GetComponentInParent<T>();
                case EffectHitTarget.Receiver:
                    // Debug.Log(" HitDataVariableOwner.ReceiverOwner", hitData.Receiver.transform);
                    return _hitDataProvider
                        .GetHitData()
                        .Receiver.transform.GetComponentInParent<T>();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
