using System;
using System.Collections.Generic;
using MonoFSM.Condition;
using UnityEngine;
using UnityEngine.Events;

using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;

namespace MonoFSM.Variable
{
    // Gameplay Attributes
    //FIXME: 不需要狀態？應該要是一個Getter, IFloatProvider
    public sealed class VarStat : VarFloat, IConditionChangeListener
    {
        private float BaseValue => Field.ProductionValue;
        private bool _isDirty = true; //set dirty?
        private float _lastBaseValue;
        private float _value;

        //FIXME: 有可能用ScriptableObject? 混用？還是monobehaviour比較好
        [PreviewInInspector] [AutoChildren] private VariableStatModifier[] _localStatModifiers; //原本就放在下面..這是不是反而不會有太多用處

        // ValueChangedListener<float> listener;
        // [PreviewInInspector] List<VariableStatModifier> statModifiers = new();

        [ShowInInspector] private List<IStatModifer> _statModifiers = new();

        protected override void Awake()
        {
            base.Awake();
            if (_localStatModifiers == null) return;
            foreach (var statModifier in _localStatModifiers) _statModifiers.Add(statModifier);
        }

        [ShowInPlayMode]
        public override float FinalValue
        {
            get
            {
                if (_isDirty || _lastBaseValue != BaseValue)
                {
                    //條件一變，值就變？dirty也是一路問，問每個statmodifier
                    ForceCalValues();
                    // listener?.OnChange(_value, false);
                }

                return _value;
            }
        }

        public override float CurrentValue
        {
            get
            {
                //FIXME: bound還要管嗎？
                if (_isDirty) ForceCalValues();
                return _value;
            }
            //FIXME: 要可以set嗎？
            // set => _lastBaseValue = value; //hmm???
        }

        private float _lastValue;

        private float ValueAfterApplyModifier()
        {
            // Debug.Log("Cal Value:" + BaseValue + "," + statModifiers.Count);
            if (Application.isPlaying == false) return CalValueAfterModifier(_localStatModifiers);

            _statModifiers?.Sort(_modifierOrder);
            return CalValueAfterModifier(_statModifiers);
        }

        ///最重要的！
        private float ForceCalValues() 
        {
            _isDirty = false;
            _lastValue = _value;
            _lastBaseValue = BaseValue;
            var tempValue = ValueAfterApplyModifier();
            if (_modifiers != null)
                foreach (var modifier in _modifiers)
                    tempValue = modifier.AfterGetValueModifyCheck(tempValue);
            _value = tempValue;
            if (_lastValue != _value) OnValueChangedRaw?.Invoke();
            return _value;
        }

        private float CalValueAfterModifier(IReadOnlyList<IStatModifer> statModifiers)
        {
            if (statModifiers == null)
                return BaseValue;
            var finalValue = BaseValue;
            float sumPercentAdd = 0;
            for (var i = 0; i < statModifiers.Count; i++)
            {
                var mod = statModifiers[i];
                Debug.Log("Stat Modifier:" + mod.GetValue + mod.GetModType + " mod.IsValid:" + mod.IsValid);
                if (mod.IsValid == false) continue;
                switch (mod.GetModType)
                {
                    case StatModType.Flat:
                        finalValue += mod.GetValue;
                        break;
                    //大部分都是這個才對
                    case StatModType.PercentAdd:
                    {
                        sumPercentAdd += mod.GetValue;

                        if (i + 1 >= statModifiers.Count || statModifiers[i + 1].GetModType != StatModType.PercentAdd)
                        {
                            finalValue *= 1 + sumPercentAdd;
                            sumPercentAdd = 0;
                        }

                        break;
                    }
                    //用得到嗎？
                    case StatModType.PercentMult:
                        //TODO: 直接乘比較好懂???
                        // finalValue *= mod.Value;
                        finalValue *= mod.GetValue;
                        break;
                    case StatModType.Overwrite:
                        finalValue = mod.GetValue;
                        break;
                }
            }

            // Workaround for float calculation errors, like displaying 12.00001 instead of 12
            return (float)Math.Round(finalValue, 4);
        }




        // public void AddListener(UnityAction<float> action, MonoBehaviour owner)
        // {
        //     if (owner == null)
        //     {
        //         // var mono = action.Target as MonoBehaviour;
        //         // if (mono == null)
        //         // {
        //         Debug.LogError("PLZ FIX ME, Assign Owner for function block!!" + action.Target);
        //         return;
        //         // }
        //         // owner = mono;
        //     }
        //
        //     //
        //     // if (listener == null)
        //     // {
        //     //     listener = new ValueChangedListener<float>();
        //     // }
        //     //
        //     // listener.AddListenerDict(action, owner);
        // }

        public void AddModifier(IStatModifer mod)
        {
            //FIXME: 真的可以這樣新增嗎？
            if (!_statModifiers.Contains(mod))
            {
                _isDirty = true;
                _statModifiers.Add(mod);
                var value = CurrentValue; //modifier改變，更新一下值
            }
            else
            {
                Debug.Log("Character Stat Already Has Modifier" + mod.GetValue + mod.GetModType);
            }
        }

        public void AddModifier(VariableStatModifier mod) //fixme: 可以用StatModifier就好嗎？
        {
            // Debug.Log("Add Stat modifier" + this);
            if (!_statModifiers.Contains(mod))
            {
                _isDirty = true;
                _statModifiers.Add(mod);
                var value = CurrentValue; //modifier改變，更新一下值
                // Debug.Log("Character Stat Add Modifier" + mod.Value + mod.Type + ",result:" + value);
            }
            else
            {
                Debug.Log("Character Stat Already Has Modifier" + mod.Value + mod._type);
            }
        }

        public bool RemoveModifier(IStatModifer mod)
        {
            if (_statModifiers.Remove(mod))
            {
                _isDirty = true;
                var value = CurrentValue; //modifier改變，更新一下值
                return true;
            }

            return false;
        }
        public bool RemoveModifier(VariableStatModifier mod)
        {
            if (_statModifiers.Remove(mod))
            {
                _isDirty = true;
                var value = CurrentValue; //modifier改變，更新一下值
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _statModifiers.Clear();
            _value = BaseValue;
            _isDirty = true;
        }

        public bool RemoveAllModifiersFromSource(IStatModifierOwner source)
        {
            //check remove from back to front of statModifiers
            for (var i = _statModifiers.Count - 1; i >= 0; i--)
            {
                if (_statModifiers[i].Source == source)
                {
                    _statModifiers.RemoveAt(i);
                    _isDirty = true;
                }
            }

            return _isDirty;
        }

        public void SetDirty()
        {
            _isDirty = true;
        }

        private readonly Comparison<IStatModifer> _modifierOrder =
            (a, b) => a.GetOrder < b.GetOrder ? -1 : a.GetOrder > b.GetOrder ? 1 : 0;

        public void OnConditionChanged()
        {
            SetDirty();
            var currentVal = ForceCalValues();
            Debug.Log("VarStat.OnConditionChanged() " + currentVal, this);
        }
    }
}