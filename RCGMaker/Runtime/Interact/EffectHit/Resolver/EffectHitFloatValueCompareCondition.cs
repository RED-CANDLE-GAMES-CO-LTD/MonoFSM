using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Runtime.Interact.EffectHit.Resolver
{
    //連System也用組的？
    //System下要做哪些事，也可能依照情境調整
    //EffectSystem, 但實際上要執行的function會是Data決定，像是扣某些數值就是Dealer決定
    //這個要Effect打過來的瞬間才可以拿到
    //目前是掛在Dealer下，拿到一個遠端的Receiver
    public abstract class AbstractEffectHitCondition : MonoBehaviour
    {
        public abstract bool IsEffectHitValid(GeneralEffectReceiver receiver);
        //FIXME: 
        // VariableProvider<float> _provider;
        // public GeneralEffectDealer dealer;
        // public VariableTag tag;
        // public AbstractMonoVariable GetMonoVariable => dealer.FindVariableOfBinder<AbstractMonoVariable>(tag);
    }

    public class EffectHitFloatValueCompareCondition : AbstractEffectHitCondition
    {
        public enum CompareType
        {
            Equal,
            Greater,
            Less,
            GreaterEqual,
            LessEqual,
        }

        public CompareType compareType;

        public VariableProvider<float> dealerVariable;

        // [DropDownRef] public VariableFloat dealerVariable;

        public VariableProvider<float> receiverVariable; //FIXME: 這個static就拿到了，要改成動態的耶...

        [PreviewInInspector] GeneralEffectReceiver _runtimeReceiver;

        public override bool IsEffectHitValid(GeneralEffectReceiver receiver)
        {
            _runtimeReceiver = receiver;
            var dealerValue = dealerVariable.Value;
            var receiverValue = receiverVariable.GetValueFrom(receiver);
            Debug.Log($"IsEffectHitValid dealerValue: {dealerValue}, receiverValue: {receiverValue}", this);
            var result = compareType switch
            {
                CompareType.Equal => dealerValue == receiverValue,
                CompareType.Greater => dealerValue > receiverValue,
                CompareType.Less => dealerValue < receiverValue,
                CompareType.GreaterEqual => dealerValue >= receiverValue,
                CompareType.LessEqual => dealerValue <= receiverValue,
                _ => false
            };
            _lastResult = result;
            return result;
        }

        [PreviewInInspector] private bool _lastResult;
    }
}