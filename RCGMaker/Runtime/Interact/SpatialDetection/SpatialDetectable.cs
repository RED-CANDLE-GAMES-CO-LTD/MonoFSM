using RCGMaker.Core.Detection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    //空間中的物件，可以被偵測到, 基本上會有collider或是collider2D
    public class SpatialDetectable : MonoBehaviour, IDefaultSerializable
    {
        //FIXME: 確保layer有設定
        [AutoChildren] GeneralEffectReceiver[] _effectReceivers;
        [ShowInInspector] public GeneralEffectReceiver[] EffectReceivers => _effectReceivers;

    }
}