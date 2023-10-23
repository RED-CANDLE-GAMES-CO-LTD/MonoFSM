using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class StateMachineOwner : MonoBehaviour, IAnimatorProvider, IResetter
{
    [AutoChildren] private GeneralFSMContext fsmContext;

    public GeneralFSMContext FsmContext =>
        fsmContext ? fsmContext : fsmContext = GetComponentInChildren<GeneralFSMContext>();
    [Title("超連結，只有prefab可以改")] [InlineEditor] [DisallowModificationsIn(PrefabKind.NonPrefabInstance)]
    public List<Component> quickFindLinks;
    

    void IResetter.EnterLevelReset()
    {
        ResetFSM();
    }

    public void ExitLevelAndDestroy()
    {
        //throw new System.NotImplementedException();
    }

    public void ResetFSM()
    {
        
        if (fsmContext.fsm == null)
            return;
        // fsmContext.ChangeState(fsmContext.startState);
        if (fsmContext.fsm.HasState(fsmContext.startState))
            fsmContext.ChangeState(fsmContext.startState);
        else
            Debug.LogError("fsmContext.startState not found?", gameObject);
    }


    public Animator ChildAnimator => GetComponentInChildren<Animator>();
}