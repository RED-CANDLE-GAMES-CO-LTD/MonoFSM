using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableStatModifier : MonoBehaviour //單一數值的modify...不同層
    {
        //還是用Variable比較好，可以被UI顯示？
        [Header("Target Stat")] public VariableFloatProvider _targetStatProvider;

        public VariableFloatProvider _valueProvider;

        public float Value => _valueProvider.Value;

        // [Range(-10, 10)] public float _value; //定值，應該不需要再用variable了才對？

        [ShowInInspector]
        string ValueDescription
        {
            get
            {
                return Type switch
                {
                    StatModType.Flat => "+" + Value,
                    StatModType.PercentAdd => $"+{Value * 100}%",
                    StatModType.PercentMult => $"*{Value * 100}%",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public StatModType Type = StatModType.Flat;
        public int Order;

        //FIXME: auto fetch, preview?
        [PreviewInInspector] IStatModifierOwner _source; //原本的parent?可以用interface?
        public IStatModifierOwner Source => _source;

        [Button]
        void Rename()
        {
            name = "Stat Modifier " + ValueDescription;
        }

        [PreviewInInspector] [AutoChildren] AbstractConditionComp[] _conditions;

        [PreviewInInspector] public bool IsValid => _conditions.IsAllValid();

        //FIXME: 監聽condition才觸發dirty? 很貴耶...
        //bool condition?
        //update檢查valid...hmmm 這裡又polling
        [AutoParent] VarStat _stat;
        private bool lastValid = false;

        //FIXME: polling dirty?
        // private void Update()
        // {
        //     if (IsValid != lastValid)
        //     {
        //         _stat.SetDirty();
        //     }
        //
        //     lastValid = IsValid;
        // }
    }

    //應該要是什麼關係...就是一個Stat? 但Variable和Stat要分開宣告嗎？ 還是就繼承？
}