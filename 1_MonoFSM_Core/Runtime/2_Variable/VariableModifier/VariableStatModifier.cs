using System;
using MonoFSM.DataProvider;
using UnityEngine;
using Sirenix.OdinInspector;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    public class VariableStatModifier : MonoBehaviour, IStatModifer //單一數值的modify...不同層
    {
        //還是用Variable比較好，可以被UI顯示？
        [BoxGroup("Target")] public VariableFloatProvider _targetStatProvider;

        [BoxGroup("Modifier")] public float _constValue;

        [BoxGroup("Modifier")] public VarFloatProviderRef _valueProvider;

        [BoxGroup("Modifier")]
        [PreviewInInspector]
        public float Value => _valueProvider?.Value ?? _constValue;

        // [SerializeField] private StatModifier _statModifier;
        // [Range(-10, 10)] public float _value; //定值，應該不需要再用variable了才對？

        [ShowInInspector]
        private string ValueDescription 
            => Type switch
            {
                StatModType.Flat => "+" + Value,
                StatModType.PercentAdd => $"+{Value * 100}%",
                StatModType.PercentMult => $"*{Value * 100}%",
                _ => throw new ArgumentOutOfRangeException()
            };

        public StatModType Type = StatModType.Flat; //Const?
        public int Order;

        //FIXME: auto fetch, preview?
        // [PreviewInInspector] IStatModifierOwner _source; //原本的parent?可以用interface?
        public Object Source => this;

        [Button]
        private void Rename()
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

        public VariableTag targetStatTag { get; }
        public int GetOrder => Order;
        public StatModType GetModType => Type;
        public float GetValue => Value;
    }

    //應該要是什麼關係...就是一個Stat? 但Variable和Stat要分開宣告嗎？ 還是就繼承？
}