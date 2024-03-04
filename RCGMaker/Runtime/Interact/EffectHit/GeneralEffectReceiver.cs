using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    //FIXME: 應該要怎麼轉接比較好，我會有好幾種事件類型，幫每種事件類型定義類別，再讓下面的action去做事
    public class GeneralEffectReceiver : MonoBehaviour, IEffectReceiver
    {
        //EnterNode?
        //Exit Node?
        [AutoChildren(DepthOneOnly = true)] EffectEnterNode _enterNode;
        [AutoChildren(DepthOneOnly = true)] EffectExitNode _exitNode;

        //收到事件後，叫下面的action做事
        public void EffectHitEnter(IEffectHitData data) //這裡是code定義
        {
            _enterNode.OnEffectReceived(data);
        }

        public void OnHitExit(IEffectHitData data)
        {
            _exitNode.OnEffectReceived(data);
        }

        //EffectExit也要呢
    }
}