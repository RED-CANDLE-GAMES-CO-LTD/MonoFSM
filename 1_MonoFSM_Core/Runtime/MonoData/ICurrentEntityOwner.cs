using System.Collections.Generic;

namespace MonoFSM.Runtime.Variable
{
    /// <summary>
    ///     「持有當前 entity」的容器：VarListEntity 的選取項、ForEachEntityInListAction 的迭代項。
    ///     VarEntityCurrentItem 用 [AutoParent] 撿最近的一顆 owner，
    ///     所以同一顆 VarEntityCurrentItem 掛在 list 底下或 foreach 底下都能自動接上。
    ///     要再加新的容器（例如對別種集合迭代的 action），實作這個介面即可，
    ///     不用再開新的 VarEntity 子型別或接 provider。
    /// </summary>
    public interface ICurrentEntityOwner
    {
        MonoEntity CurrentEntity { get; }
        string ListDescription { get; }

#if UNITY_EDITOR
        /// <summary>
        ///     最近一次「跑過哪些項目」的軌跡，只給 Inspector debug 看（掛在底下的 VarEntityCurrentItem
        ///     會鏡射顯示，不用跳回 parent）。沒有迭代語意的 owner（例如 VarListEntity）回 null。
        /// </summary>
        IReadOnlyList<MonoEntity> DebugIteratedEntities { get; }
#endif
    }
}
