using System;
using System.Collections.Generic;
// using I2.Loc;
using UnityEngine;

namespace RCGMaker.Core
{
    public class ConditionStringProvider : AbstractStringProvider
    {
        [Serializable]
        public class ConditionString
        {
            public AbstractConditionComp condition;

            [SerializeField] private string Value;

            // [SerializeField] private LocalizedString LocalizedValue;
            // public string FinalValue => string.IsNullOrEmpty(LocalizedValue.mTerm) ? Value : LocalizedValue;
            public string FinalValue => Value;
        }

        public string DefaultString;
        // private LocalizedString DefaultLocalizedValue;

        public List<ConditionString> conditionStrings = new();

        public override string StringValue
        {
            get
            {
                foreach (var conditionString in conditionStrings)
                    if (conditionString.condition.FinalResult)
                        return conditionString.FinalValue;
                // return string.IsNullOrEmpty(DefaultLocalizedValue.mTerm) ? DefaultString : DefaultLocalizedValue;
                return DefaultString;
            }
        }
    }
}