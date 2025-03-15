using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    public class EventNode:MonoBehaviour
    {
        //收到事件後，往下觸發
        
        //時間到？觸發
        //被回收，觸發？
        
        // [Component]
        // [PreviewInInspector] [AutoChildren] IRCGArgEventReceiver[] _effectReceivedProcessor;
        //
        // public void OnEventTriggered(IEffectHitData data)
        // {
        //     foreach (var processor in _effectReceivedProcessor)
        //     {
        //         processor.EventReceived(data);
        //     }
        // }
    }
}