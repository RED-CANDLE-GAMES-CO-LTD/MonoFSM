using System.Collections.Generic;
using jerryee.UnityMCP;
using RCGMaker.Runtime.FSM.RCGStateMachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGFSM.Variable
{
    //set flag, pick item...和GameFlag有關的要用一個interface才可以撈出來
    //FIXME: 需要雙向reference, debug用，要不然不知道誰在set? candidate
    public class SetVariableBoolAction : AbstractStateAction, IRCGArgEventReceiver<bool>
    {
        //FIXME: 用selection dropdown來篩選
        //這個還可以化簡嗎？整個description就代表含義了..但沒有Reference可能還是不夠用
        protected override string renamePostfix => _target ? _target.name + " to " + TargetValue : "null";

        IList<VarBool> GetVariables()
        {
            var context = GetComponentInParent<VariableOwner>(true);
            var vars = context.GetComponentsInChildren<VarBool>(true);
            return vars;
        }

        [FormerlySerializedAs("_targetFlag")]
        [FormerlySerializedAs("targetFlag")]
        [MCPExtractable]
        [DropDownRef]
        [ValueDropdown(nameof(GetVariables))]
        // [InlineEditor]
        [Required]
        // [HideIf("Multiple")]
        public VarBool _target; //var?
        //ObjectReference還指不到耶？ 
        
        //FIXME: Multiple的話另外寫SetVariableComplexAction, 直接用VariableProviderList之類的好了？
        // [ShowIf("Multiple")] public List<VarBool> targetFlags;

        [MCPExtractable]
        public bool TargetValue = true;

        // public bool Multiple = false;


        protected override void OnStateEnterImplement()
        {
            SetValue();
        }

        public override void EventReceived<T>(T arg)
        {
            // this.Log("EventReceived setVariableBoolAction");
            if (arg is bool b)
                SetValue(b);
            else
                SetValue();
        }

        void SetValue(bool v)
        {
            // if (Multiple)
            // {
            //     if (targetFlags == null)
            //         return;
            //
            //     foreach (var flag in targetFlags)
            //     {
            //         if (flag != null)
            //             flag.SetValue(v, this);
            //     }
            // }
            // else
            // {
            if (_target == null)
            {
                Debug.LogError("targetFlag==null", this);
                return;
            }

            this.Log($"SetVariableBool {_target} SetValue:{v}");
            _target.SetValue(v, this);
            // }
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