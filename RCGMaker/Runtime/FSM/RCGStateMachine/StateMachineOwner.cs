using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

public class HideFromFSMExportAttribute : PropertyAttribute
{
}

//如果有不能直接toString的結構，要客製化的serializable，就用這個...還是都用JSON會對？
public class CustomSerializableAttribute : PropertyAttribute
{
}

public static class StateMachineExtension
{
    public static T[] GetComponentsInBinder<T>(this MonoBehaviour monoBehaviour) 
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>() as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren<T>();
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<T>();
    }
}

public interface IBinder
{
    
}
public class StateMachineOwner : MonoBehaviour, IAnimatorProvider, IResetter, ILevelResetStart, IDefaultSerializable,IBinder
{

    [PreviewInInspector] [AutoChildren] private GeneralFSMContext fsmContext;
    [PreviewInInspector] [AutoChildren] private GeneralFSMContext[] fsmContexts;
    public GeneralFSMContext FsmContext =>
        fsmContext ? fsmContext : fsmContext = GetComponentInChildren<GeneralFSMContext>();

    [HideFromFSMExport]
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
        //不能有兩個進入點喔
       // ResetFSM(); //最新規, levelReset之後
    }

    [Button]
    void ExportSerializedData()
    {

    }
    
    
}