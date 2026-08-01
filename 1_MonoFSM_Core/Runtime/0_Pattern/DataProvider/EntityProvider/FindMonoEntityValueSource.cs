using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    public enum EntityLookupMode
    {
        /// <summary>從 source entity 的 sub-scope 找 children (source._ownBinder)</summary>
        Sub,
        /// <summary>從 source entity 所屬 scope 找 siblings (source._parentBinder)</summary>
        Sibling,
    }

    /// <summary>
    /// 從指定的 source VarEntity (持有一個 MonoEntity) 開始，
    /// 依 _lookupMode 走 sub-binder 或 parent-binder，
    /// 用 _expectedEntityTag 找對應 MonoEntity 作為自己的 Value。
    /// 透過 MonoEntityBinder 查找，不依賴 transform tree 階層、也不用 GetComponentsInChildren。
    /// </summary>
    public class FindMonoEntityValueSource : AbstractEntitySource
    {
        [PropertyOrder(-1)]
        [DropDownRef]
        [SerializeField]
        private VarEntity _sourceVarEntity;

        [PropertyOrder(-1)]
        [SerializeField]
        private EntityLookupMode _lookupMode = EntityLookupMode.Sibling;

        public override string SuggestDeclarationName =>
            _expectedEntityTag != null ? _expectedEntityTag.name : "FindEntity";

        [ShowInPlayMode]
        public override MonoEntity monoEntity
        {
            get
            {
                if (_sourceVarEntity == null || _expectedEntityTag == null) return null;
                var sourceEntity = _sourceVarEntity.Value;
                if (sourceEntity == null) return null;
                return _lookupMode == EntityLookupMode.Sub
                    ? sourceEntity.GetSubEntity(_expectedEntityTag)
                    : sourceEntity.GetSiblingEntity(_expectedEntityTag);
            }
        }
    }
}
