using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGFSM.Variable
{
    //set flag, pick item...和GameFlag有關的要用一個interface才可以撈出來
    public class SetFlagBoolAction : AbstractStateAction, IRCGArgEventReceiver
    {
        [HideIf("Multiple")] public VariableBool targetFlag;

        [ShowIf("Multiple")] public List<VariableBool> targetFlags;

        public bool TargetValue = true;

        public bool Multiple = false;


        protected override void OnStateEnterImplement()
        {
            SetValue();
        }

        public void EventReceived<T>(T arg)
        {
            SetValue();
        }

        void SetValue()
        {
            if (Multiple)
            {
                if (targetFlags == null)
                    return;

                foreach (var flag in targetFlags)
                {
                    if (flag != null)
                        flag.SetValue(TargetValue, this);
                }
            }
            else
            {
                if (targetFlag == null)
                {
                    Debug.LogError("targetFlag==null", this);
                    return;
                }

                targetFlag.SetValue(TargetValue, this);
            }
        }
    }

    // public class SetPropertyAction : AbstractAction
    // {
    //     [Filter(Properties = true, Fields = true)]
    //     public UnityMember property;

    //     //寫在哪裡？
    //     public float argFloat = 0;
    //     public int argInt = 0;

    //     protected override void OnStateEnterImplement()
    //     {
    //         var paramTypes = property.parameterTypes;

    //         if (paramTypes[0] == typeof(int))
    //         {
    //             property.Set(argInt);
    //         }
    //     }

    // }
}