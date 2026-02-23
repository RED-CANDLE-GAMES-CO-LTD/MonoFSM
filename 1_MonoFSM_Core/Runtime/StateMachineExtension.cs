using System;
using MonoFSM.CustomAttributes;
using MonoFSM.Variable;
using UnityEngine;

public class HideFromFSMExportAttribute : PropertyAttribute { }

//如果有不能直接toString的結構，要客製化的serializable，就用這個...還是都用JSON會對？
public class CustomSerializableAttribute : PropertyAttribute { }

//FIXME: 需要這個嗎？
public interface IMonoEntity : IDropdownRoot
{
    VariableFolder VariableFolder { get; }
    public AbstractMonoVariable GetVar(VariableTag varTag);
}

public static class StateMachineExtension
{
    public static T FindVariableOfBinder<T>(this MonoBehaviour monoBehaviour, VariableTag type)
        where T : class
    {
        //FIXME: 效能
        if (monoBehaviour == null)
        {
            Debug.LogError("monoBehaviour is null");
            return default;
        }

        var owner = monoBehaviour.GetComponentInParent<IMonoEntity>(); //被monoDescriptable擋掉了...
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
        if (binder != null)
            return binder.GetComponentInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return default;
    }

    //FIXME: 效能不好？editor code沒差
    public static Component[] GetComponentsOfSibling(
        this Component monoBehaviour,
        Type parentType,
        Type siblingType
    )
    {
        var binder = monoBehaviour.GetComponentInParent(parentType) as MonoBehaviour;
        if (binder != null)
            return binder.GetComponentsInChildren(siblingType);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<Component>();
    }

    public static T GetComponentInBinder<T>(this MonoBehaviour monoBehaviour)
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null)
            return binder.GetComponentInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return default;
    }

    public static T[] GetComponentsInBinder<T>(this Component monoBehaviour)
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null)
            return binder.GetComponentsInChildren<T>(true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<T>();
    }

    public static Component[] GetComponentsInBinder(this Component monoBehaviour, Type type)
    {
        var binder = monoBehaviour.GetComponentInParent<IBinder>(true) as MonoBehaviour;
        if (binder != null)
            return binder.GetComponentsInChildren(type, true);
        Debug.LogError("IBinder not found", monoBehaviour);
        return Array.Empty<Component>();
    }
}

public interface IBinder { }
