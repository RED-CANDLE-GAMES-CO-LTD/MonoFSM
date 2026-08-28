using UnityEngine;

namespace MonoFSM.Core.Variable
{
    /// <summary>
    ///     Component 清單的 Var（VarList&lt;Component&gt;）。常用來裝一組場上節點（例如 Transform 點位），
    ///     由 CurrentIndex 指出「目前選中哪一個」，CurrentListItem 取回那顆 Component。
    ///     index 要跨端一致時，把 _currentIndexVar 綁一顆掛 NetworkedVarTag 的 VarInt。
    ///     ValueType 是 List&lt;Component&gt;，所以跨 entity 用 varTag 取它時拿到的是整份 list 而不是單一元素。
    /// </summary>
    public class VarListComp : VarList<Component>
    { }
}
