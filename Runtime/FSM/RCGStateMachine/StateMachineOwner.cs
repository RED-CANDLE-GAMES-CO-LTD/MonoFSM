using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace RCGMaker.Core
{
    public class StateMachineOwner : MonoBehaviour//, IPoolObject
    {
        [AutoChildren] GeneralFSMContext fsmContext;

        [Title("超連結，最好給prefab改就好")] [InlineEditor]
        public List<Component> quickFindLinks;

        public void EnterLevelResetAndStart()
        {
            ResetFSM();
        }

        // void IPoolObject.PoolBeforeDestroy()
        // {
        //     // throw new System.NotImplementedException();
        // }
        //
        // void IPoolObject.PoolOnDestroy()
        // {
        //     // fsmContext.ChangeState(fsmContext.startState);
        // }
        //
        // void IPoolObject.PoolOnPrepared(PoolObject poolObj)
        // {
        //     // throw new System.NotImplementedException();
        // }
        //
        // void IResetter.EnterLevelResetAndStart()
        // {
        //     ResetFSM();
        // }

        private void ResetFSM()
        {
            if (fsmContext.fsm == null)
                return;
            // fsmContext.ChangeState(fsmContext.startState);
            if (fsmContext.fsm.HasState(fsmContext.startState))
            {
                fsmContext.ChangeState(fsmContext.startState);
            }
            else
            {
                Debug.LogError("fsmContext.startState not found?", this.gameObject);
            }
        }
    }
}