using System;
using MonoFSM.Condition;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    public class VariableStatModifier : AbstractDescriptionBehaviour, IStatModifer //單一數值的modify...不同層
    {
        [AutoParent]
        private IConditionChangeListener _inParent; //遠遠的註冊就沒有這個？還是註冊時assign

        //還是用Variable比較好，可以被UI顯示？
        // [BoxGroup("Target")] public VariableFloatProvider _targetStatProvider;

        // [BoxGroup("Modifier")] [CompRef] [Auto]
        // public VarFloatProviderRef _valueProvider; //FIXME: 不該用這個ㄅ？

        // [Required]
        // [BoxGroup("Modifier")] [CompRef] [Auto]
        // private IValueProvider<float>
        //     _floatProvider; //來源

        [BoxGroup("Modifier")]
        [SerializeField]
        [ValidateInput(nameof(ValidateValueVarNotAncestor),
            "_valueVar 不可指向自己 parent 鏈上的 VarFloat/VarStat，會造成循環依賴 (StackOverflow)")]
        private VarFloatWrapper _valueVarRef = new(1f); //預設常數值 1，沿用舊的 null fallback 行為

        //modifier 掛在某個 VarStat 底下；若 _valueVar 是該 VarStat 或其祖先，
        //計算 stat 時會透過此 modifier 繞回自己，造成無限遞迴
        private bool ValidateValueVarNotAncestor(VarFloatWrapper wrapper)
        {
            if (wrapper?._var == null)
                return true;
            return !transform.IsChildOf(wrapper._var.transform);
        }

        [BoxGroup("Modifier")]
        [PreviewInInspector]
        public float FinalValue
        {
            get
            {
                if (IsDirty || Application.isPlaying == false)
                {
                    _cachedProviderValue = _valueVarRef.Value;
                    _cachedFinalValue = _cachedProviderValue * _valueMultiplier;
                }

                return _cachedFinalValue;
            }
        }

        private float _cachedProviderValue; //這個是用來顯示的

        private float _cachedFinalValue;

        [SerializeField]
        private float _valueMultiplier = 1f;

        private string sign => FinalValue >= 0 ? "+" : "-"; //這個是用來顯示的

        //只有指向 VarFloat 時才顯示來源名稱，否則常數值會和 ValueDescription 重複顯示（"1 +1"）
        public override string Description =>
            _valueVarRef._var != null ? _valueVarRef._var.name + " " + ValueDescription : ValueDescription;

        [ShowInInspector]
        private string ValueDescription =>
            _type switch
            {
                StatModType.Flat => $"{sign}{Mathf.Abs(FinalValue)}",
                StatModType.PercentAdd => $"{sign}{Mathf.Abs(FinalValue) * 100}%",
                StatModType.PercentMult => $"* {FinalValue * 100}%",
                _ => throw new ArgumentOutOfRangeException(),
            }; //FIXME: 用value不對ㄅ provider的資訊

        [FormerlySerializedAs("Type")]
        public StatModType _type = StatModType.Flat; //Const?

        [FormerlySerializedAs("Order")]
        public int _order;

        //FIXME: auto fetch, preview?
        // [PreviewInInspector] IStatModifierOwner _source; //原本的parent?可以用interface?
        public Object Source => this;

        protected override string DescriptionTag => "Stat M";

        // [PreviewInInspector]
        [CompRef]
        [AutoChildren]
        AbstractConditionBehaviour[] _conditions;

        [PreviewInInspector] public bool IsValid => isActiveAndEnabled && _conditions.IsAllValid();

        //FIXME: 怪怪的？
        public bool IsDirty =>
            Application.isPlaying ? _cachedProviderValue != _valueVarRef.Value : false;

        //FIXME: 監聽condition才觸發dirty? 很貴耶...
        //bool condition?
        //update檢查valid...hmmm 這裡又polling
        [AutoParent]
        VarStat _stat;
        private bool _lastValid = false;

        public VariableTag targetStatTag { get; }
        public int GetOrder => _order;
        public StatModType GetModType => _type;
        public float GetValue => FinalValue;
    }

    //應該要是什麼關係...就是一個Stat? 但Variable和Stat要分開宣告嗎？ 還是就繼承？
}
