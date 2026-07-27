using System.Collections.Generic;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.PrefabCache
{
    // 掛在 prefab root 上，存檔時把這份 prefab 的精簡階層文字寫成 markdown cache。
    // 輸出目錄由專案端設定（PrefabTextCacheWriter.CacheRoot）。
    //
    // 為什麼需要：離線讀 .prefab YAML 讀不到 variant 繼承來的內容 —— Unity 只在
    // 本檔有引用時才寫出 stripped 佔位，那些節點的名稱、component、真值全部只存在
    // base prefab 裡。走 Unity 匯出的是「合併後」的樹，沒有這個問題。
    // cache 是本機產物（不進 git），價值在於「落成檔案才能 grep / 分段讀」——
    // 大 prefab 匯出動輒數十 KB，直接回傳會整份吃掉 LLM 的 context。
    [DisallowMultipleComponent]
    public class PrefabTextCacheMarker : MonoBehaviour, IBeforePrefabSaveCallbackReceiver,
        ICustomPrefabSaveCallbackReceiver
    {
        [Tooltip("關掉就不寫 cache，但保留下面的設定")]
        public bool _cacheEnabled = true;

        [Tooltip("不摺疊已知子樹（StateFolder / VariableFolder …）。輸出會大很多，只有需要看完整欄位時才開")]
        public bool _fullExpand;

        [Tooltip("要展開的子樹路徑（相對 prefab root）。尾端加 /* 代表整棵展開")]
        [ListDrawerSettings(ShowFoldout = false)]
        public List<string> _expandPaths = new();

        [Tooltip("額外附一段 FSM markdown（沒有 StateFolder 的 prefab 開了也不會有內容）")]
        public bool _exportFsm = true;

        [Title("瘦身")]
        [Tooltip("排除 Renderer / ParticleSystem / AudioSource / IK 等純視覺 component。" +
                 "PPlayer 實測省掉約 1/3，且骨架與特效節點會因此變成純 Transform 而被連鎖摺疊")]
        public bool _excludeVisual = true;

        [Tooltip("inactive 子樹摺成一行。PPlayer 的 inactive 行佔 17%，但 MonoFSM 有些邏輯物件" +
                 "本來就是 inactive 的，開之前確認一下")]
        public bool _foldInactive;

        [Tooltip("單一 component 的欄位文字上限，超過補 …(+N more)。0 = 用 exporter 預設（400）")]
        public int _maxFieldCharsPerComponent;

        [Tooltip("超過這個深度的子樹摺成一行 Name (+N nodes)。cache 是目錄不是全文，" +
                 "要看細節走 ExportSubtree 現撈。-1 = 不限深度（整份輸出會很大）")]
        public int _maxDepth = 6;

        // 實作在 MonoFSM.Core.Editor，runtime 這邊參照不到，由 PrefabTextCacheWriter
        // 在 [InitializeOnLoadMethod] 時注入
        public static System.Action<PrefabTextCacheMarker> CacheWriter;
        public static System.Func<PrefabTextCacheMarker, string> CachePathResolver;

        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(10)]
        [LabelText("輸出路徑")]
        private string CachePathPreview
        {
            get => CachePathResolver?.Invoke(this) ?? "(存檔後才知道)";
        }

        public void OnBeforePrefabSave() => WriteCache();

        public void OnCustomPrefabSave() => WriteCache();

        [Button("立刻寫出 cache")]
        [PropertyOrder(11)]
        private void WriteCache() => CacheWriter?.Invoke(this);
    }
}
