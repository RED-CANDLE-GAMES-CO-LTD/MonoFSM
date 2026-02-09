using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Resolver
{
    //連System也用組的？
    //System下要做哪些事，也可能依照情境調整
    //EffectSystem, 但實際上要執行的function會是Data決定，像是扣某些數值就是Dealer決定
    //這個要Effect打過來的瞬間才可以拿到
    //目前是掛在Dealer下，拿到一個遠端的Receiver
    public abstract class AbstractEffectHitCondition : AbstractDescriptionBehaviour
    {
        protected override string DescriptionTag => "EffectHitCondition";

        const int MaxResultRecordCount = 10;

        [Serializable]
        public struct ResultRecord
        {
            [HorizontalGroup, HideLabel] public float _time;
            [HorizontalGroup, HideLabel] public bool _result;
            [HorizontalGroup, HideLabel] public EffectResolver _receiverName;

            public ResultRecord(float time, bool result, EffectResolver receiver)
            {
                _time = time;
                _result = result;
                _receiverName = receiver;
            }

            public override string ToString() =>
                $"[{_time:F2}s] {(_result ? "Hit" : "Miss")} {_receiverName}";
        }

        [ShowInDebugMode] [ShowInInspector, ReadOnly] [ListDrawerSettings(IsReadOnly = true)]
        List<ResultRecord> _resultRecords = new();

        public bool IsEffectShouldHit(EffectResolver receiver)
        {
            if (gameObject.activeSelf == false)
                return true; //條件不啟用就當作通過
            var result = IsEffectHitValidImplement(receiver);
            if (_resultRecords.Count >= MaxResultRecordCount)
                _resultRecords.RemoveAt(0);
            _resultRecords.Add(new ResultRecord(Time.time, result, receiver));

            return result;
        }


        //應該用EffectHitData?
        protected abstract bool IsEffectHitValidImplement(EffectResolver receiver); //只能在Dealer用？


        //FIXME:
        // VariableProvider<float> _provider;
        // public GeneralEffectDealer dealer;
        // public VariableTag tag;
        // public AbstractMonoVariable GetMonoVariable => dealer.FindVariableOfBinder<AbstractMonoVariable>(tag);
    }

    // public class EffectHitFloatValueCompareCondition : AbstractEffectHitCondition
    // {
    //     public enum CompareType
    //     {
    //         Equal,
    //         Greater,
    //         Less,
    //         GreaterEqual,
    //         LessEqual,
    //     }
    //
    //     public CompareType compareType;
    //
    //     public VariableFloatProvider dealerVariable;
    //
    //     // [DropDownRef] public VariableFloat dealerVariable;
    //
    //     public VariableFloatProvider receiverVariable; //FIXME: 這個static就拿到了，要改成動態的耶...
    //
    //     [PreviewInInspector] GeneralEffectReceiver _runtimeReceiver;
    //
    //     [PreviewInInspector]
    //     protected override string description =>
    //         $"Compare dealer:{dealerVariable._varTag.name} {compareType} receiver:{receiverVariable._varTag.name}";
    //
    //     public override bool IsEffectHitValid(GeneralEffectReceiver receiver)
    //     {
    //         _runtimeReceiver = receiver;
    //         var dealerValue = dealerVariable.Value;
    //         if (receiverVariable == null)
    //         {
    //             Debug.LogError("receiverVariable is null", this);
    //             return false;
    //         }
    //
    //         var receiverValue = receiverVariable.GetValueFrom(receiver);
    //         Debug.Log($"IsEffectHitValid dealerValue: {dealerValue}, receiverValue: {receiverValue}", this);
    //         // Debug.Log("receiver", receiver);
    //         var result = compareType switch
    //         {
    //             CompareType.Equal => dealerValue == receiverValue,
    //             CompareType.Greater => dealerValue > receiverValue,
    //             CompareType.Less => dealerValue < receiverValue,
    //             CompareType.GreaterEqual => dealerValue >= receiverValue,
    //             CompareType.LessEqual => dealerValue <= receiverValue,
    //             _ => false
    //         };
    //         _lastResult = result;
    //         return result;
    //     }
    //
    //     [PreviewInInspector] private bool _lastResult;
    // }
}
