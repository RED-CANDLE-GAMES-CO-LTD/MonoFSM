using System;

using UnityEngine;

using Sirenix.OdinInspector;

namespace MonoFSM.Core
{
//給編輯Prefab時，能快速的過濾掉不需要的Component
//專注編輯目前關心的Component
    [CreateAssetMenu(fileName = "FilterPreset", menuName = "RCG/ScriptableObject/FilterPreset")]
#if UNITY_EDITOR
    public class FilterPreset : SerializedScriptableObject
#else
    public class FilterPreset : ScriptableObject
#endif
    {
        public Type ModuleEntityType;
        public Type[] allowTypes;
    }
}
