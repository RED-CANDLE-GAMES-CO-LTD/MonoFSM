using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    /// <summary>
    /// 根據 Rigidbody 的速度來估算距離的一種 ValueSource，適用於需要根據物體移動速度來調整檢測距離的情況。
    /// </summary>
    public class DistanceSourceFromSpeed : AbstractValueSource<float>
    {
        public Rigidbody _rigidbody;

        public float _minDis = 0.5f;

        //FIXME: init speed?
        //要用上個frame的速度嗎? 最好是把它記下來？
        public float Distance =>
            _rigidbody != null ? _rigidbody.linearVelocity.magnitude + _minDis : 0f;
        public override float Value => Distance;
    }
}
