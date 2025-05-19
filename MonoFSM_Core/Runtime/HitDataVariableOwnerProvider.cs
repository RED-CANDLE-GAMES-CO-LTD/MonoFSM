using MonoFSM.Variable;
using UnityEngine;

public class HitDataVariableOwnerProvider : MonoBehaviour,IVariableOwnerProvider
{
    
    [AutoParent]
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
}
