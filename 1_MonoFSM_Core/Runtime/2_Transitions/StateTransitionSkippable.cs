using UnityEngine;

//裝在Projectile 上標記他不能被Skip
public class StateTransitionSkippable : MonoBehaviour , ISkippableAnimationTransition
{
    public bool canSkip = true;
    public bool CanSkip() 
        => canSkip;
}
    
public interface ISkippableAnimationTransition
{
    bool CanSkip();
}