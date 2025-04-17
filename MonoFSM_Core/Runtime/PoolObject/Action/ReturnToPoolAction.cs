using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.ObjectPool
{
    public class ReturnToPoolAction : AbstractStateAction
    {
        [Required]
        [PreviewInInspector]
        [AutoParent] PoolObject _poolObject;
        protected override void OnStateEnterImplement()
        {
            _poolObject.ReturnToPool();
        }
    }
}