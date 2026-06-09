using System;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._3_FlagData.DataFunction
{
    [Serializable]
    public class PreviewDataFunction : AbstractDataFunction
    {
        public MonoObj PreviewPrefab => _previewPrefab;

        [PrefabFilter]
        [SerializeField]
        private MonoObj _previewPrefab; //純 View 顯示用，用 simulator.SpawnVisual / DespawnVisual 生成回收
    }
}
