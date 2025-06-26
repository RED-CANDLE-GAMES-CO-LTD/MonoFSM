using System;
using JetBrains.Annotations;
using MonoFSM.Condition;
using MonoFSM.DataProvider;
using UnityEngine;
using Sirenix.OdinInspector;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable.Attributes;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    public class VariableStatModifier : MonoBehaviour, IStatModifer //單一數值的modify...不同層
    {
        [AutoParent] private IConditionChangeListener _inParent; //遠遠的註冊就沒有這個？還是註冊時assign
        //還是用Variable比較好，可以被UI顯示？
        // [BoxGroup("Target")] public VariableFloatProvider _targetStatProvider;

        // [BoxGroup("Modifier")] [CompRef] [Auto]
        // public VarFloatProviderRef _valueProvider; //FIXME: 不該用這個ㄅ？

        [BoxGroup("Modifier")] [CompRef] [Auto]
        private IFloatProvider
            _floatProvider;

        [BoxGroup("Modifier")]
        [PreviewInInspector]
        public float Value
        {
            get
            {
                if (IsDirty)
                {
                    _cachedProviderValue = _floatProvider?.Value ?? 0f;
                    _cachedValue = _cachedProviderValue * _valueMultiplier;
                }

                return _cachedValue;
            }
        }

        private float _cachedProviderValue; //這個是用來顯示的
        private float _cachedValue;
        //_valueProvider?.Value * _valueMultiplier ?? _valueMultiplier;

        // 新增事件
        // public event Action OnValueChanged;

        // valueMultiplier 改為 property
        [SerializeField] private float _valueMultiplier = 1f;

        //FIXME: AddListener, RemoveListener
        //這個好醜！
        // public UnityAction OnValueChanged
        // {
        //     get => _valueProvider.VarRaw.OnValueChangedRaw;
        //     set => _valueProvider.VarRaw.OnValueChangedRaw = value;
        // }

        private string sign => Value >= 0 ? "+" : "-"; //這個是用來顯示的
        [ShowInInspector]
        private string ValueDescription //FIXME: 用value不對ㄅ provider的資訊
            => _type switch
            {
                StatModType.Flat => $"{sign}{Mathf.Abs(Value)}",
                StatModType.PercentAdd => $"{sign}{Mathf.Abs(Value) * 100}%",
                StatModType.PercentMult => $"*{Value * 100}%",
                _ => throw new ArgumentOutOfRangeException()
            };

        [FormerlySerializedAs("Type")] public StatModType _type = StatModType.Flat; //Const?
        [FormerlySerializedAs("Order")] public int _order;

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
        public bool IsDirty => Application.isPlaying ? _cachedProviderValue != _floatProvider.Value : false;

        //FIXME: 監聽condition才觸發dirty? 很貴耶...
        //bool condition?
        //update檢查valid...hmmm 這裡又polling
        [AutoParent] VarStat _stat;
        private bool _lastValid = false;

        public VariableTag targetStatTag { get; }
        public int GetOrder => _order;
        public StatModType GetModType => _type;
        public float GetValue => Value;

    }

    //應該要是什麼關係...就是一個Stat? 但Variable和Stat要分開宣告嗎？ 還是就繼承？
}