using UnityEngine;
using Sirenix.OdinInspector;

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
        this.TryGetComp<IOnEnableInvokable>()?.OnEnableInvoke();
        // if (IsParentInvoke)
        transform.parent.TryGetComp<IOnEnableInvokable>()?.OnEnableInvoke();
    }

    private void OnDisable()
    {
        this.TryGetComp<IOnEnableInvokable>()?.OnDisableInvoke();
        // if (IsParentInvoke)
        transform.parent.TryGetComp<IOnEnableInvokable>()?.OnDisableInvoke();
    }
}