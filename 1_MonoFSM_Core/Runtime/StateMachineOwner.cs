using System;
using System.Collections.Generic;
using Fusion.Addons.FSM;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

public class HideFromFSMExportAttribute : PropertyAttribute
{
}

//如果有不能直接toString的結構，要客製化的serializable，就用這個...還是都用JSON會對？
public class CustomSerializableAttribute : PropertyAttribute
{
}

//FIXME: 需要這個嗎？
public interface IVarBlackboard
{
    public string name { get; }
    VariableFolder VariableFolder { get; }
    public AbstractMonoVariable GetVar(VariableTag varTag);
    GameObject gameObject { get; }
}

public static class StateMachineExtension
{
    public static T FindVariableOfBinder<T>(this MonoBehaviour monoBehaviour, VariableTag type) where T : class
    {
        //FIXME: 效能
        if (monoBehaviour == null)
        {
            Debug.LogError("monoBehaviour is null");
            return default;
        }

        var owner = monoBehaviour.GetComponentInParent<IVarBlackboard>(); //被monoDescriptable擋掉了...
        if (owner == null)
        {
            Debug.LogError("IVariableOwner not found", monoBehaviour);
            return default;
        }

        var folder = owner.VariableFolder;
        if (folder == null)
        {
            Debug.LogError("VariableFolder not found", owner as MonoBehaviour);
            return default;
        }

        return folder.GetVariable(type) as T;
        // return GetComponentOfSibling<StateMachineOwner, RCGVariableFolder>(monoBehaviour).GetVariable(type);
    }

    public static T GetComponentOfSibling<TParent, T>(this MonoBehaviour monoBehaviour)
    {
        //FIXME: 效能不好?
        var binder = monoBehaviour.GetComponentInParent<TParent>() as MonoBehaviour;
        if (binder != null) return binder.GetComponentInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return default;
    }

    //FIXME: 效能不好？editor code沒差
    public static Component[] GetComponentsOfSibling(this Component monoBehaviour, Type parentType, Type siblingType)
    {
        var binder = monoBehaviour.GetComponentInParent(parentType) as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren(siblingType);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<Component>();
    }

    /// <summary>
    /// 找到所有的parent下的sibling
    /// </summary>
    /// <param name="monoBehaviour"></param>
    /// <param name="parentType"></param>
    /// <param name="siblingType"></param>
    /// <returns></returns>
    public static Component[] GetComponentsOfSiblingAll(this Component monoBehaviour, Type parentType, Type siblingType)
    {
        //FIXME: 這個需要到multiple parent嗎？
        var parents = monoBehaviour.GetComponentsInParent(parentType);
        var list = new List<Component>();
        if (parents == null || parents.Length == 0)
        {
            Debug.LogError("parent Type not found:" + parentType, monoBehaviour);
            return Array.Empty<Component>();
        }


        foreach (var binder in parents)
        {
            var comps = binder.GetComponentsInChildren(siblingType, true);

            list.AddRange(comps);
        }
        
        return list.ToArray();
        // if (binder != null) return binder.GetComponentsInChildren(siblingType);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<Component>();
    }

    public static IList<T> GetComponentsOfSibling<TParent, T>(this MonoBehaviour monoBehaviour)
    {
        var binder = monoBehaviour.GetComponentInParent<TParent>() as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<T>();
    }

    public static IList<Component> GetComponentsOfSibling<TParent>(this MonoBehaviour monoBehaviour, Type type)
    {
        var binder = monoBehaviour.GetComponentInParent<TParent>() as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren(type, true);
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

    public static T[] GetComponentsInBinder<T>(this Component monoBehaviour)
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<T>();
    }

    public static Component[] GetComponentsInBinder(this Component monoBehaviour, Type type)
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null) return binder.GetComponentsInChildren(type, true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<Component>();
    }
}

public interface IBinder
{
}

//FIXME: 沒用了？
[Obsolete]
public class StateMachineOwner : MonoBehaviour, IAnimatorProvider, IDefaultSerializable,
    IBinder
{
    // [PreviewInInspector][AutoChildren] private GeneralFSMContext fsmContext;
    // [PreviewInInspector] [AutoChildren] private GeneralFSMContext[] fsmContexts;

    [AutoChildren] public StateMachineLogic fsmLogic;

    // public GeneralFSMContext FsmContext =>
    //     fsmContext ? fsmContext : fsmContext = GetComponentInChildren<GeneralFSMContext>();

    // [HideFromFSMExport]
    // [Title("超連結，只有prefab可以改")]
    // [InlineEditor]
    // [DisallowModificationsIn(PrefabKind.NonPrefabInstance)]
    // public List<Component> quickFindLinks;

    // public void ResetFSM()
    // {
    //     if (fsmContext == null)
    //         Debug.LogError("fsmContext is null", gameObject);
    //
    //     if (fsmContext.fsm == null)
    //     {
    //         Debug.LogError("fsmContext.fsm is null", gameObject);
    //         return;
    //     }
    //
    //     // fsmContext.ChangeState(fsmContext.startState);
    //     if (fsmContext.fsm.HasState(fsmContext.startState))
    //         fsmContext.ChangeState(fsmContext.startState);
    //     else
    //         Debug.LogError("fsmContext.startState not found?", gameObject);
    //     _hasReset = true;
    // }
    //
    // public void PauseAll()
    // {
    //     foreach (var context in fsmContexts) context.PauseFSM();
    // }
    //
    // public void ResumeAll()
    // {
    //     foreach (var context in fsmContexts) context.ResumeFSM();
    // }

    public Animator ChildAnimator => GetComponentInChildren<Animator>();

    public Animator[] ChildAnimators => GetComponentsInChildren<Animator>();

    // private void Start()
    // {
    //     // ResetFSM(); //中途加入的玩家沒有call到這個
    // }
    //2. 關卡重置後開始

    // void IResetStart.ResetStart() //Instaniate之後不會call這個...
    // {
    //     //不能有兩個進入點喔
    //     ResetFSM(); //最新規, levelReset之後, 
    //
    // }

    // [ShowInPlayMode]
    // [InfoBox("FSM has not been reset!", InfoMessageType.Error, nameof(HasNotReset))]
    // private bool _hasReset = false;

    // private bool HasNotReset => !_hasReset;


    [Button]
    private void ExportSerializedData()
    {
    }

    // [PreviewInInspector]
    // [AutoChildren]
    // RCGVariableFolder _variableFolder;
    // public RCGVariableFolder VariableFolder
    // {
    //     get
    //     {
    //         #if UNITY_EDITOR
    //         if (Application.isPlaying == false && _variableFolder == null)
    //         {
    //             _variableFolder = GetComponentInChildren<RCGVariableFolder>();
    //         }
    //         #endif
    //         return _variableFolder;
    //     }
    // }
    // public void EnterSceneStart()
    // {
    //     // ResetFSM();
    // }
}