using System.Collections.Generic;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;

/// <summary>
/// 提供VariableOwner(可能會從一些奇怪的地方拿到)
/// </summary>
/// FIXME: 叫IMonoEntityProvider?
public interface IMonoEntityProvider //不可以提供value, 要不然會和後續的打架
{
    public MonoEntity monoEntity { get; }
    public MonoEntityTag entityTag { get; } //editorTime就要有了
    public T GetComponentOfOwner<T>(); //這個不該獨立？
    public string Description => "VariableOwnerProvider"; //可以覆寫


}


//FIXME: global instance也應該抽出來