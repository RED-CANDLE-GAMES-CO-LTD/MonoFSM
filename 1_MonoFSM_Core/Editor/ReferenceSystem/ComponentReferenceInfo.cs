using MonoFSM.Runtime;
using UnityEngine;

namespace MonoFSM.Editor.ReferenceSystem
{
    public enum ReferenceScope
    {
        Local,
        CrossEntity
    }

    public enum ReferenceType
    {
        DirectField,
        VarWrapper,
        ValueProvider
    }

    public class ComponentReferenceInfo
    {
        /// <summary>
        /// 被引用的目標 Object
        /// </summary>
        public Object Target;

        /// <summary>
        /// 引用此 Object 的 Component
        /// </summary>
        public Component ReferencingComponent;

        /// <summary>
        /// 欄位路徑 (例如: "_targetHandler" 或 "_wrapper._var")
        /// </summary>
        public string FieldPath;

        /// <summary>
        /// 引用方式
        /// </summary>
        public ReferenceType Type;

        /// <summary>
        /// 引用範圍 (同 Entity / 跨 Entity)
        /// </summary>
        public ReferenceScope Scope;

        /// <summary>
        /// 引用來源所屬的 MonoEntity
        /// </summary>
        public MonoEntity OwnerEntity;

        public string TypeDisplayName => Type switch
        {
            ReferenceType.DirectField => "Direct",
            ReferenceType.VarWrapper => "Wrapper",
            ReferenceType.ValueProvider => "Provider",
            _ => "Unknown"
        };

        public string ComponentDisplayName =>
            ReferencingComponent != null
                ? $"{ReferencingComponent.gameObject.name} ({ReferencingComponent.GetType().Name})"
                : "(null)";
    }
}
