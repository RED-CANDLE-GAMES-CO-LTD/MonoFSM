using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableStatModifier : MonoBehaviour //單一數值的modify...不同層
    {
        [Range(-10, 10)] public float Value; //定值，應該不需要再用variable了才對？
        [ShowInInspector] string ValueDescription => (Value * 100) + "%";
        public StatModType Type = StatModType.PercentAdd;
        public int Order;

        //FIXME: auto fetch, preview?
        [PreviewInInspector] IStatModifierOwner _source; //原本的parent?可以用interface?
        public IStatModifierOwner Source => _source;
        [Button]
        void Rename()
        {
            name = "Stat Modifier "+ ValueDescription;
        }
        
    }

    //應該要是什麼關係...就是一個Stat? 但Variable和Stat要分開宣告嗎？ 還是就繼承？
}