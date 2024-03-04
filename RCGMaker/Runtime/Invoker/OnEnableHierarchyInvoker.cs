using UnityEngine;
using Sirenix.OdinInspector;

//FIXME: 這個不太健康，用下面的
public interface IOnEnableInvokable
{
    void OnEnableInvoke();
    void OnDisableInvoke();
}



public interface IDataOwner
{
    void FlagGeneratedPostProcess(GameFlagBase flag);

    // public string Name { get; }
    public string name { get; }
    public Transform transform { get; }
}

public class OnEnableHierarchyInvoker : MonoBehaviour
{
    // public bool IsParentInvoke = true;
    [InfoBox("打開我可以讓parent(上面)的IOnEnableInvokable(FxPlayer)噴噴")]
    private void OnEnable()
    {
        //FIXME: ref 應該要先拿起來吧？
        // this.TryGetComp<IOnEnableInvokable>()?.OnEnableInvoke();
        // // if (IsParentInvoke)
        // transform.parent.TryGetComp<IOnEnableInvokable>()?.OnEnableInvoke();
        _onEnableInvokable?.OnEnableInvoke();
    }

    [AutoParent] private IOnEnableInvokable _onEnableInvokable;

    private void OnDisable()
    {
        // this.TryGetComp<IOnEnableInvokable>()?.OnDisableInvoke();
        // // if (IsParentInvoke)
        // transform.parent.TryGetComp<IOnEnableInvokable>()?.OnDisableInvoke();
        _onEnableInvokable?.OnDisableInvoke();
    }
}