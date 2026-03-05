using UnityEngine;

namespace MonoFSM.Foundation
{
    public abstract class AbstractWorldFollowBehaviour : AbstractDescriptionBehaviour
    {
        public Transform _followTarget;

        public virtual Transform FollowTransform => _followTarget;
    }
}
