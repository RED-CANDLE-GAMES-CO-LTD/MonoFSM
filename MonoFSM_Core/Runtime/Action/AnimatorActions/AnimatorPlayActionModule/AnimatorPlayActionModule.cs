using UnityEngine;

using RCGFSM.Animation;

namespace RCGFSM.AnimatorControl
{
    public abstract class AnimatorPlayActionModule : MonoBehaviour
    {
        [AutoParent] public AnimatorPlayAction animatorPlayAction;
    }
}