using System;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.FSM._2_Variable.VirutalizeVariable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    [Serializable]
    public class ValueWrapper
    {
        [FormerlySerializedAs("valueFrom")] public Component valueFromRenamed;
        public float Value => ((IFloatValue)valueFromRenamed).FinalValue;
    }
    public class GeneralEffectDealer : EffectResolver, IEffectDealer
    {
        public EffectValue effectValue;

        private IEnumerable<Component> GetIFloatValue()
        {
            var comps = GetComponentsInChildren<IFloatValue>();
            return comps.Select(c => (Component)c);
        }

        [FormerlySerializedAs("valueProvider")] [ValueDropdown("GetIFloatValue")] [DropDownRef]
        public Component valueFrom;

        [FormerlySerializedAs("valueWrapper")] public ValueWrapper valueWrapperRenmae;

        [FormerlySerializedAs("valueWrapper")] [SerializeReference]
        public ValueWrapper valueWrapper2;


        [SerializeReference] public IFloatValue valueRef;

        private IFloatValue _valueProvider => (IFloatValue)valueFrom;
        
        public bool CanHitReceiver(IEffectReceiver receiver)
        {
            return ((GeneralEffectReceiver)receiver).EffectType == EffectType;
        }

        public float FinalValue => _valueProvider.FinalValue;


        public void OnHitEnter(IEffectHitData data)
        {
            _enterNode?.OnEffectReceived(data);
        }

        public void OnHitExit(IEffectHitData data)
        {
            _exitNode?.OnEffectReceived(data);
        }
    }
}