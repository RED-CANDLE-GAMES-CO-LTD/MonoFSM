using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.ObjectPool
{
    //FIXME: Despawn action?
    public class ReturnToPoolAction : AbstractStateAction
    {
        [Required] [PreviewInInspector] [AutoParent]
        private MonoPoolObj _poolObject;

        protected override void OnActionExecuteImplement()
        {
            _poolObject.Despawn();
        }
    }
}