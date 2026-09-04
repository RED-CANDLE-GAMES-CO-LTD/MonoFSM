using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Foundation
{
    public abstract class AbstractWorldFollowBehaviour : AbstractDescriptionBehaviour
    {
        [Required]
        public Transform _followTarget;

        public virtual Transform FollowTransform => _followTarget;
    }
}
