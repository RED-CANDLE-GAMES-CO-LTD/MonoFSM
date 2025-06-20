using MonoFSM.Core;
using MonoFSM.Runtime.FSM.RCGStateMachine;
using UnityEngine;

/// <summary>
/// 提供VariableOwner(可能會從一些奇怪的地方拿到)
/// </summary>
public interface IBlackboardProvider //不可以提供value, 要不然會和後續的打架
{
    public MonoBlackboard Blackboard { get; }
    public T GetComponentOfOwner<T>(); //這個不該獨立？
    public string Description => "VariableOwnerProvider"; //可以覆寫
}


//FIXME: global instance也應該抽出來