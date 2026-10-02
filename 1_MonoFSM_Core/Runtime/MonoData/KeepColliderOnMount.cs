using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    /// <summary>
    /// 標記：同一個 GameObject 上的 Collider 在 ViewRoot.MountTo(disableColliders: true) 時不要被關掉。
    /// 用在「整台被 mount 走、但某幾顆 trigger 還是要能被碰到」的情況，
    /// 例：電動野狼被騎著時（PPlayer Ride 會關掉機車整棵 collider），後座發電鴿收納的取出 / 收鴿 trigger 還要能用。
    /// 只看同一個 GameObject，不會往下影響子節點；沒掛的 collider 照舊關掉。
    /// </summary>
    [DisallowMultipleComponent]
    public class KeepColliderOnMount : MonoBehaviour
    {
    }
}
