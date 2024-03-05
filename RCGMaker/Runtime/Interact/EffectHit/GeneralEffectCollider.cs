using RCGMaker.Core.Detection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class GeneralEffectCollider : MonoBehaviour
    {
        //FIXME: 確保layer有設定
        [AutoChildren] GeneralEffectReceiver[] _effectReceivers;
        [ShowInInspector] public GeneralEffectReceiver[] EffectReceivers => _effectReceivers;


        [ShowInInspector]
        public int Layer
        {
            get => gameObject.layer;
            set => gameObject.layer = value;
        }
    }
}