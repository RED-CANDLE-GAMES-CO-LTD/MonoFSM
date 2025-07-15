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

        // [Required]
        [BoxGroup("Modifier")] [CompRef] [Auto]
        private IValueProvider<float>
            _floatProvider; //來源

        [BoxGroup("Modifier")]
        [PreviewInInspector]
        public float FinalValue
        {
            get
            {
                if (IsDirty || Application.isPlaying == false)
                {
                    _cachedProviderValue = _floatProvider?.Value ?? 1f;
                    _cachedFinalValue = _cachedProviderValue * _valueMultiplier;
                }

                return _cachedFinalValue;
            }
        }

        private float _cachedProviderValue; //這個是用來顯示的

        private float _cachedFinalValue;
        [SerializeField] private float _valueMultiplier = 1f;
        
        private string sign => FinalValue >= 0 ? "+" : "-"; //這個是用來顯示的
        [ShowInInspector]
        private string ValueDescription //FIXME: 用value不對ㄅ provider的資訊
            => _type switch
            {
                StatModType.Flat => $"{sign}{Mathf.Abs(FinalValue)}",
                StatModType.PercentAdd => $"{sign}{Mathf.Abs(FinalValue) * 100}%",
                StatModType.PercentMult => $"*{FinalValue * 100}%",
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

        [PreviewInInspector] [AutoChildren] AbstractConditionBehaviour[] _conditions;

        [PreviewInInspector] public bool IsValid => _conditions.IsAllValid();
        public bool IsDirty => Application.isPlaying ? _cachedProviderValue != _floatProvider?.Value : false;

        //FIXME: 監聽condition才觸發dirty? 很貴耶...
        //bool condition?
        //update檢查valid...hmmm 這裡又polling
        [AutoParent] VarStat _stat;
        private bool _lastValid = false;

        public VariableTag targetStatTag { get; }
        public int GetOrder => _order;
        public StatModType GetModType => _type;
        public float GetValue => FinalValue;

    }

    //應該要是什麼關係...就是一個Stat? 但Variable和Stat要分開宣告嗎？ 還是就繼承？
}