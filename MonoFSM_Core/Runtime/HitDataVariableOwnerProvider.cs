using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using UnityEngine;

/// <summary>
/// 提供VariableOwner(可能會從一些奇怪的地方拿到), 必須要有HitDataProvider
/// </summary>
public class HitDataVariableOwnerProvider : MonoBehaviour,IVariableOwnerProvider
{
    [CompRef] [AutoParent]
    IHitDataProvider hitDataProvider;


    public enum HitDataVariableOwner
    {
        DealerOwner,
        ReceiverOwner,
    }

    public HitDataVariableOwner ownerType;



    public IVariableOwner GetVariableOwner()
    {

        if (Application.isPlaying == false)
            return null;
        
        switch (ownerType)
        {
            case HitDataVariableOwner.DealerOwner:
                
                Debug.Log(" HitDataVariableOwner.DealerOwner",hitDataProvider.GetHitData().Dealer.transform);
                return hitDataProvider.GetHitData().Dealer.transform.GetComponentInParent<IVariableOwner>();
            case HitDataVariableOwner.ReceiverOwner:
                Debug.Log(" HitDataVariableOwner.ReceiverOwner",hitDataProvider.GetHitData().Receiver.transform);
                return hitDataProvider.GetHitData().Receiver.transform.GetComponentInParent<IVariableOwner>();
            default:
                throw new System.NotImplementedException();
        }
        
    }


    public T GetComponentOfOwner<T>() //好像有點白痴
    {
        var owner = GetVariableOwner();
        if (owner == null)
            return default;
        return owner.gameObject.GetComponent<T>();
    }
}
