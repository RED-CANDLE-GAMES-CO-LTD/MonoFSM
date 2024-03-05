using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

public class HideFromSerializationAttribute : PropertyAttribute
{
}

//如果有不能直接toString的結構，要客製化的serializable，就用這個...還是都用JSON會對？
public class CustomSerializableAttribute : PropertyAttribute
{
}

public class StateMachineOwner : MonoBehaviour, IAnimatorProvider, IResetter, ILevelResetStart, IDefaultSerializable
{
    [PreviewInInspector] [AutoChildren] private GeneralFSMContext fsmContext;

    public GeneralFSMContext FsmContext =>
        fsmContext ? fsmContext : fsmContext = GetComponentInChildren<GeneralFSMContext>();

    [HideFromSerialization]
    [Title("超連結，只有prefab可以改")] [InlineEditor] [DisallowModificationsIn(PrefabKind.NonPrefabInstance)]
    public List<Component> quickFindLinks;

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
    public Animator[] ChildAnimators => GetComponentsInChildren<Animator>();

    public void EnterLevelReset() //舊的九日code, enter levelReset
    {
         ResetFSM(); //應該要選一邊？之後砍掉這裏？還是這邊不call，九日還是跑下面的？
    }

    public void ExitLevelAndDestroy() //舊的九日code, enter levelReset
    {
       
    }

    void ILevelResetStart.LevelResetStart()
    {
        ResetFSM(); //最新規, levelReset之後
    }

    [Button]
    void ExportSerializedData()
    {

    }
    
    
}