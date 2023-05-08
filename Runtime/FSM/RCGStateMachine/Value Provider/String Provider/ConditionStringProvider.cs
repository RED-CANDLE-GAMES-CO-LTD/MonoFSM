using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCGMaker.Core
{
    public class ConditionStringProvider : AbstractStringProvider
    {
        [Serializable]
        public class ConditionString
        {
            public AbstractConditionComp condition;
            public string Value;
        }

        public string DefaultString;

        public List<ConditionString> conditionStrings = new();

        public override string StringValue
        {
            get
            {
                foreach (var conditionString in conditionStrings)
                    if (conditionString.condition.FinalResult)
                        return conditionString.Value;
                return DefaultString;
            }
        }
    }
}