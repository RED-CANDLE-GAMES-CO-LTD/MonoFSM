using System;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    //用這個觸發action?
    public class EffectEnterNode : MonoBehaviour, IEffectReceivedHandler, IDefaultSerializable,IActionParent
    {
        [Component]
        [PreviewInInspector] [AutoChildren(DepthOneOnly = true)]
        private IRCGArgEventReceiver<IEffectHitData>[] _effectReceivedProcessor = Array.Empty<IRCGArgEventReceiver<IEffectHitData>>();

        public void OnEffectReceived(IEffectHitData data) //FIXME: 還需要interface嗎？ interface可以給別人寫...
        {
            // Debug.Log(" EffectEnterNode OnEffectReceived", this);
            foreach (var processor in _effectReceivedProcessor)
            {
                //FIXME: 應該要判定enable?           
                if(processor.isActiveAndEnabled)
                    processor.EventReceived(data);
            }
        }
        
        
    }
}