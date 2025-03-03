using System.Collections.Generic;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGFSM.Variable
{
    //set flag, pick item...和GameFlag有關的要用一個interface才可以撈出來
    //FIXME: 需要雙向reference, debug用，要不然不知道誰在set? candidate
    public class SetVariableBoolAction : AbstractStateAction, IRCGArgEventReceiver<bool>
    {
        //FIXME: 用selection dropdown來篩選
        protected override string renamePostfix => targetFlag ? targetFlag.name + " to " + TargetValue : "null";

        IList<VarBool> GetVariables()
        {
            var context = GetComponentInParent<VariableOwner>(true);
            var vars = context.GetComponentsInChildren<VarBool>(true);
            return vars;
        }

        [DropDownRef]
        [ValueDropdown(nameof(GetVariables))]
        // [InlineEditor]
        [Required]
        [HideIf("Multiple")]
        public VarBool targetFlag;

        [ShowIf("Multiple")] public List<VarBool> targetFlags;

        public bool TargetValue = true;

        public bool Multiple = false;


        protected override void OnStateEnterImplement()
        {
            SetValue();
        }

        public override void EventReceived<T>(T arg)
        {
            this.Log("EventReceived setVariableBoolAction");
            if (arg is bool b)
                SetValue(b);
            else
                SetValue();
        }

        void SetValue(bool v)
        {
            if (Multiple)
            {
                if (targetFlags == null)
                    return;

                foreach (var flag in targetFlags)
                {
                    if (flag != null)
                        flag.SetValue(v, this);
                }
            }
            else
            {
                if (targetFlag == null)
                {
                    Debug.LogError("targetFlag==null", this);
                    return;
                }

                targetFlag.SetValue(v, this);
            }
        }

        void SetValue()
        {
            SetValue(TargetValue);
        }

        public void EventReceived(bool arg)
        {
            SetValue(arg);
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