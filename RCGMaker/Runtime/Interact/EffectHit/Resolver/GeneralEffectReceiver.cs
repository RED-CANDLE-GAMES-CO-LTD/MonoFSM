using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    //FIXME: 應該要怎麼轉接比較好，我會有好幾種事件類型，幫每種事件類型定義類別，再讓下面的action去做事
    public class GeneralEffectReceiver : EffectResolver, IEffectReceiver
    {
        public IEffectHitData GenerateEffectHitData(IEffectDealer dealer, IEffectReceiver receiver)
        {
            //FIXME: 要用pool, 泛用的pool
            var data = new GeneralEffectHitData();
            data.Override(dealer, receiver);
            return data;
        }
        
        //收到事件後，叫下面的action做事
        public IEffectType getEffectType => EffectType;

        //FIXME: rename to OnHitEnter
        public void OnEffectHitEnter(IEffectHitData data) //這裡是code定義
        {
            Debug.Log("OnEffectHitEnter", this);
            _currentHitData = data;
            _enterNode?.OnEffectReceived(data);
        }
        [PreviewInInspector]
        IEffectHitData _currentHitData;

        public void OnEffectHitExit(IEffectHitData data)
        {
            Debug.Log("OnHitExit", this);
            _exitNode?.OnEffectReceived(data);
            _currentHitData = null;
        }

        //EffectExit也要呢
    }
}