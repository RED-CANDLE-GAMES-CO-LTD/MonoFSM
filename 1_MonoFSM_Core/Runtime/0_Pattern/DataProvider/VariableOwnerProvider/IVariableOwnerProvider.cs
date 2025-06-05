using UnityEngine;

/// <summary>
/// 提供VariableOwner(可能會從一些奇怪的地方拿到)
/// </summary>
public interface IVariableOwnerProvider
{
    public IVariableOwner GetVariableOwner();
    public T GetComponentOfOwner<T>();
    public string Description => "VariableOwnerProvider"; //可以覆寫
}


//FIXME: global instance也應該抽出來