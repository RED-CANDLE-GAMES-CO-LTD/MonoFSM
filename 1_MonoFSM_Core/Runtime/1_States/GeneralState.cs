using System.Collections.Generic;
using System.Threading;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion.Addons.FSM;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

//FIXME: 可以拿掉？
public interface IGuidEntity { }

public interface ISerializableComponent
{
    public string Serialize();
    public void Deserialize(string data);
}

public interface IDefaultSerializable { }

public interface IReferenceTarget //FIXME: 這樣只有我自己寫的型別可以用？
{ }

[Searchable]
public class GeneralState : MonoStateBehaviour
{
#if UNITY_EDITOR
    [Button("Find References (New)"), PropertyOrder(-99)]
    private void FindReferencesNew()
    {
        var windowType = System.Type.GetType(
            "MonoFSM.Editor.ReferenceSystem.ComponentReferenceWindow, MonoFSM.Core.Editor");
        if (windowType != null)
        {
            var method = windowType.GetMethod("ShowWindowWithTarget",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, new object[] { this });
        }
        else
        {
            Debug.LogWarning(
                "ComponentReferenceWindow not found. Please ensure MonoFSM.Core.Editor assembly is loaded.");
        }
    }
#endif
    [SerializeField] [SOConfig("StateTags")]
    private List<StateTag> _stateTags = new();

    public bool HasTag(StateTag tag) => _stateTags.Contains(tag);

    [ShowInInspector] public float statusTimer => Machine?.ActiveState == this ? Machine.StateTime : -1f;



    [Button("強制跳State (無視條件)")]
    private void TestGoToState()
    {
        Debug.Log($"ForceActivateState to {Name}", this);
        // 使用 RestoreState 機制，會在網路同步後執行，避免被 ReadNetworkData 覆蓋
        Owner.RestoreState(StateId);
        // context.RestoreState(StateId);
    }

    //FIXME: 要區分 render 和 一般的？ 同一份code怎麼區分？從 MonoObj 去判 authority嗎？
    protected virtual void OnStateEnter() { }

    protected override void OnEnterState()
    {
        base.OnEnterState();
        OnStateEnter();
    }

    protected override void OnExitState()
    {
        base.OnExitState();
        StateExitCancellationTokenSource?.Cancel();
    }

    private CancellationTokenSource StateExitCancellationTokenSource;

    public CancellationTokenSource GetStateExitCancellationTokenSource()
    {
        if (StateExitCancellationTokenSource == null)
        {
            StateExitCancellationTokenSource = new CancellationTokenSource();
        }
        else if (StateExitCancellationTokenSource.IsCancellationRequested)
        {
            StateExitCancellationTokenSource.Dispose();
            StateExitCancellationTokenSource = new CancellationTokenSource();
        }

        return StateExitCancellationTokenSource;
    }
}
