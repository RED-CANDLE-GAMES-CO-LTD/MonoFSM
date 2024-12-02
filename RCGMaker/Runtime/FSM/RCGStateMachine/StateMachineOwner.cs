using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
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
    public static T FindVariableOfBinder<T>(this MonoBehaviour monoBehaviour, VariableTag type) where T : class
    {
        return monoBehaviour.GetComponentInParent<StateMachineOwner>().VariableFolder.GetVariable(type) as T;
        // return GetComponentOfSibling<StateMachineOwner, RCGVariableFolder>(monoBehaviour).GetVariable(type);
    }
    public static T GetComponentOfSibling<TParent, T>(this MonoBehaviour monoBehaviour) 
    {
        var binder = monoBehaviour.GetComponentInParent<TParent>() as MonoBehaviour;
        if (binder != null) return binder.GetComponentInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return default;
    }
    
    public static IList<T> GetComponentsOfSibling<TParent, T>(this MonoBehaviour monoBehaviour) 
    {
        var binder = monoBehaviour.GetComponentInParent<TParent>() as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<T>();
    }
    public static IList<Component> GetComponentsOfSibling<TParent>(this MonoBehaviour monoBehaviour,Type type) 
    {
        var binder = monoBehaviour.GetComponentInParent<TParent>() as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren(type,true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<Component>();
    }
    
    public static T GetComponentInBinder<T>(this MonoBehaviour monoBehaviour) 
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null) return binder.GetComponentInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return default;
    }
    
    public static T[] GetComponentsInBinder<T>(this MonoBehaviour monoBehaviour) 
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<T>();
    }
    public static Component[] GetComponentsInBinder(this MonoBehaviour monoBehaviour, Type type) 
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren(type,true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<Component>();
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
    //1. 關卡重置
    public void EnterLevelReset() //舊的九日code, enter levelReset
    {
        //太早了
        // ResetFSM(); //應該要選一邊？之後砍掉這裏？還是這邊不call，九日還是跑下面的？
    }

    public void ExitLevelAndDestroy() //舊的九日code, enter levelReset
    {
       
    }
    
   
    //2. 關卡重置後開始

    void ILevelResetStart.LevelResetStart()
    {
        //不能有兩個進入點喔
       ResetFSM(); //最新規, levelReset之後, 
       
    }

    [Button]
    void ExportSerializedData()
    {

    }
    
    [PreviewInInspector]
    [AutoChildren]
    RCGVariableFolder variableFolder;
    public RCGVariableFolder VariableFolder => variableFolder;
    
}