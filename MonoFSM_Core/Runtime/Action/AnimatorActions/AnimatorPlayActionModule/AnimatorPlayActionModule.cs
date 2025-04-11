using RCGFSM.Animation;
using UnityEngine;

namespace RCGFSM.AnimatorControl
{
    public abstract class AnimatorPlayActionModule : MonoBehaviour
    {
        [AutoParent] public AnimatorPlayAction animatorPlayAction;
    }
}