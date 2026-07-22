using System.Collections.Generic;
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

    /// <summary>
    /// 引用者的角色分類 — Action（會執行行為，較重要）優先顯示
    /// </summary>
    public enum ReferenceCategory
    {
        Action,
        Condition,
        Getter,
        Other
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

        /// <summary>
        /// 引用者的角色分類
        /// </summary>
        public ReferenceCategory Category;

        public string CategoryDisplayName => Category switch
        {
            ReferenceCategory.Action => "Action",
            ReferenceCategory.Condition => "Condition",
            ReferenceCategory.Getter => "Getter",
            _ => "Other"
        };

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

        /// <summary>
        /// 引用者的 Transform 階層路徑，往上走到最近的 MonoEntity 為界（若無則到 root）。
        /// 例如: "Door Entity/Pivot/Handle"
        /// </summary>
        public string HierarchyPath
        {
            get
            {
                if (ReferencingComponent == null) return "";

                var names = new List<string>();
                var current = ReferencingComponent.transform;
                while (current != null)
                {
                    names.Add(current.name);
                    // 走到帶有 MonoEntity 的節點即停（含該節點），對齊引用範圍語意
                    if (current.GetComponent<MonoEntity>() != null) break;
                    current = current.parent;
                }

                names.Reverse();
                return string.Join("/", names);
            }
        }
    }
}
