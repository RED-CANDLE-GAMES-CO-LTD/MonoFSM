using System;
using System.Collections.Generic;

using UnityEngine;

namespace MonoFSM.Core
{
    public class ConditionStringProvider : AbstractStringProvider
    {
        [Serializable]
        public class ConditionString
        {
            public AbstractConditionBehaviour condition;

            [SerializeField] private string Value;

            public string FinalValue => Value;
        }

        public string DefaultString;

        public List<ConditionString> conditionStrings = new();

        public override string StringValue
        {
            get
            {
                foreach (var conditionString in conditionStrings)
                    if (conditionString.condition.FinalResult)
                        return conditionString.FinalValue;
                return DefaultString;
            }
        }
    }
}