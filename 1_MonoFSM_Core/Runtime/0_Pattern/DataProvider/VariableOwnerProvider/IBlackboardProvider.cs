using System.Collections.Generic;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;

/// <summary>
/// 提供VariableOwner(可能會從一些奇怪的地方拿到)
/// </summary>
/// FIXME: 叫IMonoEntityProvider?
public interface IBlackboardProvider //不可以提供value, 要不然會和後續的打架
{
    public MonoBlackboard Blackboard { get; }
    public MonoEntityTag entityTag { get; }
    public T GetComponentOfOwner<T>(); //這個不該獨立？
    public string Description => "VariableOwnerProvider"; //可以覆寫

    IEnumerable<ValueDropdownItem<VariableTag>> GetParentVariableTags()
    {
        var tagDropdownItems = new List<ValueDropdownItem<VariableTag>>();
#if UNITY_EDITOR
        var tags = entityTag.containsVariableTypeTags;
        foreach (var tempTag in tags)
            tagDropdownItems.Add(new ValueDropdownItem<VariableTag>(tempTag.name, tempTag));

#endif
        return tagDropdownItems;
    }
}


//FIXME: global instance也應該抽出來