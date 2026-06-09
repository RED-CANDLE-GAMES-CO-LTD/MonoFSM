using MonoFSM.Core.Attributes;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     從 VarListEntity 取指定 index 的 MonoEntity（_index = -1 時取 CurrentListItem）。
    ///     掛在 VarEntity 下作為其來源，或當 GetVarFromParentEntitySource 的 _overrideSourceEntity 目標。
    ///     取代 [Obsolete] 的 CurrentItemOfListSource（只能 current index）。
    /// </summary>
    public class EntityFromListIndexProvider : AbstractEntityProvider
    {
        public override string SuggestDeclarationName =>
            _varList != null ? _varList.name : "listItem";

        [PropertyOrder(-1)]
        [DropDownRef]
        [SerializeField]
        private VarList<MonoEntity> _varList;

        [SerializeField]
        private VarIntWrapper _index = new(-1);

        [ShowInPlayMode]
        public override MonoEntity monoEntity =>
            _varList != null ? _varList.GetItemAt(_index.Value) : null;
    }
}
