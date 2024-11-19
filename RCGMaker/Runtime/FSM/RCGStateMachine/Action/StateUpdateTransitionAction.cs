using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGFSM.Transition
{
    //UpdateTransitionCheckAction?
    //讓transition下面有condition不就結束了？ 單層condition
    //FIXME: 被動的？不用action而是監聽的transition?
    //StateEnter, Update的時候，檢查能不能去某個state
    [RequireComponent(typeof(AbstractStateTransition))]
    public class StateUpdateTransitionAction : AbstractStateAction
    {
        [PreviewInInspector]
        [Auto]
        AbstractStateTransition validTransition;

        protected override void OnStateEnterImplement()
        {
            // Debug.Log("Action State 'Enter' Implement", gameObject);
            if (validTransition == null)
                return;

            if (validTransition.TransitionCheck())
            {
                // Debug.Break();
                //過去了
                return;
            }
        }

        protected override void OnStateUpdateImplement()
        {
            // if (validTransition == null)
            //     validTransition = this.GetComponent<AbstractStateTransition>();

            // Debug.Log("Action State 'Update' Implement", gameObject);
            if (validTransition == null)
                return;

            if (validTransition.TransitionCheck())
            {
                // Debug.Break();
                //過去了
                return;
            }
        }
    }
}