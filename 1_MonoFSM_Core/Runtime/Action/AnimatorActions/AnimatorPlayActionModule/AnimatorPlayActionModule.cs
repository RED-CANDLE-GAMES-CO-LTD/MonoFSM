using MonoFSM.Animation;
using UnityEngine;

namespace MonoFSM.AnimatorControl
{
    /// <summary>
    /// 就裝在AnimatorPlayAction的Child?
    /// </summary>
    public abstract class AnimatorPlayActionModule : MonoBehaviour
    {
        [AutoParent] protected AnimatorPlayAction _animatorPlayAction;
    }
}