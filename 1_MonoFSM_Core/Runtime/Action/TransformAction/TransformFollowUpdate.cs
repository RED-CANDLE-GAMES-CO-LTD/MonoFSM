using MonoFSM.Core.Simulate;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.TransformAction
{
    /// <summary>
    /// FIXME: TransformFollower?
    /// </summary>
    public class TransformFollowUpdate : MonoBehaviour, IUpdateSimulate
    {
        public Transform _controlTransform; //來源transform
        public Transform _targetTransform; //目標transform

        //收走要把_targetTransform拿回來耶
        public void Simulate(float deltaTime)
        {
            _controlTransform.position = _targetTransform.position;
            _controlTransform.rotation = _targetTransform.rotation;
        }

        public void AfterUpdate()
        {
        }
    }
}
