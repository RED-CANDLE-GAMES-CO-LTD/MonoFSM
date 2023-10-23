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
        
        if (this.isActiveAndEnabled == false)
        {
            //沒被打開的ＦＳＭ也不該初始化？
            //拿掉這個左右青蛙會錯
            //被Culling 掉的物件透過 存擋點reset會導致Condition 全錯（因為關著Condition 全false?）。
            
            return;
        }

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