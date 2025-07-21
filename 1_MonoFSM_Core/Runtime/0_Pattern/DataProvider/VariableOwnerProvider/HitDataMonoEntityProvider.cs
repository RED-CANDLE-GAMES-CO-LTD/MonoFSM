using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// FIXME: 繼承下面的
/// 提供VariableOwner(可能會從一些奇怪的地方拿到), 必須要有HitDataProvider
/// </summary>
/// FIXME: 依照parent就能決定要用Dealer還是Receiver的Blackboard
public class HitDataMonoEntityProvider : MonoBehaviour, IMonoEntityProvider //這個介面很怪？VariableOwner...那就直接I
{
    //可是這裡
    [CompRef] [AutoParent] private IHitDataProvider _hitDataProvider;


    public enum HitDataVariableOwner
    {
        Dealer, //rename?
        Receiver
    }

    string IMonoEntityProvider.Description => $"{ownerType}'s Blackboard";

    public HitDataVariableOwner ownerType;

    public MonoEntity monoEntity
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

            var hitData = _hitDataProvider.GetHitData();
            if (hitData == null)
                // Debug.LogError("HitData is null in HitDataVariableOwnerProvider", this);
                return null;
            switch (ownerType)
            {
                case HitDataVariableOwner.Dealer:

                    Debug.Log(" HitDataVariableOwner.DealerOwner", hitData.Dealer.transform);
                    return hitData.Dealer.transform.GetComponentInParent<MonoEntity>();
                case HitDataVariableOwner.Receiver:
                    Debug.Log(" HitDataVariableOwner.ReceiverOwner", hitData.Receiver.transform);
                    return hitData.Receiver.transform.GetComponentInParent<MonoEntity>();
                default:
                    throw new System.NotImplementedException();
            }
        }
    }

    public MonoEntityTag entityTag => monoEntity?.Tag;

    [ShowInDebugMode] private IEffectHitData currentHitData => _hitDataProvider?.GetHitData();
    

    public T GetComponentOfOwner<T>() //好像有點白痴
    {
        var owner = monoEntity;
        if (owner == null)
            return default;
        return owner.gameObject.GetComponent<T>();
    }
}

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    /// 從HitDataProvider，從hitData 來拿到 Dealer/Receiver 的 Parent Component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class HitDataParentCompProvider<T> : MonoBehaviour
    {
        [Required] [CompRef] [AutoParent] private IHitDataProvider _hitDataProvider;

        public enum HitDataTargetType
        {
            Dealer,
            Receiver
        }

        [FormerlySerializedAs("ownerType")] public HitDataTargetType _targetType;

// #if UNITY_EDITOR
        [PreviewInInspector] private IEffectHitData HitData => _hitDataProvider.GetHitData();
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
                case HitDataTargetType.Dealer:

                    // Debug.Log(" HitDataVariableOwner.DealerOwner", hitData.Dealer.transform);
                    return _hitDataProvider.GetHitData().Dealer.transform.GetComponentInParent<T>();
                case HitDataTargetType.Receiver:
                    // Debug.Log(" HitDataVariableOwner.ReceiverOwner", hitData.Receiver.transform);
                    return _hitDataProvider.GetHitData().Receiver.transform.GetComponentInParent<T>();
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}