using System;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace MonoFSM.Variable
{
    // Gameplay Attributes
    //FIXME: 不需要狀態？應該要是一個Getter, IFloatProvider
    public sealed class VarStat : VarFloat
    {
        private float BaseValue => CurrentValue;
        private bool isDirty = true;
        private float lastBaseValue;
        private float _value;
        [PreviewInInspector] [AutoChildren] VariableStatModifier[] LocalStatModifiers; //原本就放在下面..這是不是反而不會有太多用處

        ValueChangedListener<float> listener;
        [PreviewInInspector] List<VariableStatModifier> statModifiers = new();

        private List<IStatModifer> _statModifiers;

        protected override void Awake()
        {
            base.Awake();
            if (LocalStatModifiers != null)
                statModifiers.AddRange(LocalStatModifiers);
        }

        [ShowInPlayMode]
        public override float FinalValue
        {
            get
            {
                if (isDirty || lastBaseValue != BaseValue)
                {
                    //條件一變，值就變？dirty也是一路問，問每個statmodifier
                    CalValues();
                    listener?.OnChange(_value, false);
                }

                return _value;
            }
        }


        private void CalValues()
        {
            lastBaseValue = BaseValue;
            _value = CalculateFinalValue();
            isDirty = false;
        }

        private float CalValueAfterModifier(IReadOnlyList<VariableStatModifier> statModifiers)
        {
            if (statModifiers == null)
                return BaseValue;
            var finalValue = BaseValue;
            float sumPercentAdd = 0;

            //FIXME: mod必須要先把Owner灌進去？這樣才能拿到正確的targetStat和value
            // foreach (var mod in _statModifiers)
            // {
            //     if (mod.GetModType == StatModType.Flat)
            //         finalValue += mod.GetValue;
            //     else if (mod.GetModType == StatModType.PercentAdd)
            //     {
            //         sumPercentAdd += mod.GetValue;
            //         finalValue *= 1 + sumPercentAdd;
            //         sumPercentAdd = 0;
            //     }
            //     else if (mod.GetModType == StatModType.PercentMult)
            //         finalValue *= mod.GetValue;
            // }

            for (var i = 0; i < statModifiers.Count; i++)
            {
                var mod = statModifiers[i];
                if (mod.IsValid == false) continue;
                if (mod.Type == StatModType.Flat)
                {
                    finalValue += mod.Value;
                }
                else if (mod.Type == StatModType.PercentAdd) //大部分都是這個才對
                {
                    sumPercentAdd += mod.Value;

                    if (i + 1 >= statModifiers.Count || statModifiers[i + 1].Type != StatModType.PercentAdd)
                    {
                        finalValue *= 1 + sumPercentAdd;
                        sumPercentAdd = 0;
                    }
                }
                else if (mod.Type == StatModType.PercentMult) //用得到嗎？
                {
                    //TODO: 直接乘比較好懂???
                    // finalValue *= mod.Value;
                    finalValue *= mod.Value;
                }
            }

            // Workaround for float calculation errors, like displaying 12.00001 instead of 12
            return (float)Math.Round(finalValue, 4);
        }


        private float CalculateFinalValue()
        {
            // Debug.Log("Cal Value:" + BaseValue + "," + statModifiers.Count);
            if (Application.isPlaying == false)
            {
                return CalValueAfterModifier(LocalStatModifiers);
            }

            statModifiers?.Sort(_modifierOrder);
            return CalValueAfterModifier(statModifiers);
        }

        public void AddListener(UnityAction<float> action, MonoBehaviour owner)
        {
            if (owner == null)
            {
                // var mono = action.Target as MonoBehaviour;
                // if (mono == null)
                // {
                Debug.LogError("PLZ FIX ME, Assign Owner for function block!!" + action.Target);
                return;
                // }
                // owner = mono;
            }


            if (listener == null)
            {
                listener = new ValueChangedListener<float>();
            }

            listener.AddListenerDict(action, owner);
        }

        public void AddModifier(IStatModifer mod)
        {
            //FIXME: 真的可以這樣新增嗎？
            if (!_statModifiers.Contains(mod))
            {
                isDirty = true;
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
            if (!statModifiers.Contains(mod))
            {
                isDirty = true;
                statModifiers.Add(mod);
                var value = CurrentValue; //modifier改變，更新一下值
                // Debug.Log("Character Stat Add Modifier" + mod.Value + mod.Type + ",result:" + value);
            }
            else
            {
                Debug.Log("Character Stat Already Has Modifier" + mod.Value + mod.Type);
            }
        }

        public bool RemoveModifier(VariableStatModifier mod)
        {
            if (statModifiers.Remove(mod))
            {
                isDirty = true;
                var value = CurrentValue; //modifier改變，更新一下值
                return true;
            }

            return false;
        }

        public void Clear()
        {
            statModifiers.Clear();
            _value = BaseValue;
            isDirty = true;
        }

        public bool RemoveAllModifiersFromSource(IStatModifierOwner source)
        {
            //check remove from back to front of statModifiers
            for (var i = statModifiers.Count - 1; i >= 0; i--)
            {
                if (statModifiers[i].Source == source)
                {
                    statModifiers.RemoveAt(i);
                    isDirty = true;
                }
            }

            return isDirty;
        }

        public void SetDirty()
        {
            isDirty = true;
        }

        private Comparison<VariableStatModifier> _modifierOrder =
            (a, b) => a.Order < b.Order ? -1 : a.Order > b.Order ? 1 : 0;
    }
}