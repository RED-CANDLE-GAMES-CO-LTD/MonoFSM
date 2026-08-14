using System.Collections.Generic;
using System.Text;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.ReferenceSystem
{
    /// <summary>
    ///     VarTagUsageScanner 的視窗殼：選一個 MonoEntityTag / VariableTag → Scan →
    ///     摘要表 + 按「來源檔案」分組的明細，每組可點擊 Ping 到對應 prefab / scene。
    /// </summary>
    public class VarTagUsageWindow : OdinEditorWindow
    {
        [MenuItem("Tools/MonoFSM/Var Tag Usage")]
        public static void ShowWindow()
        {
            var window = Open();

            if (Selection.activeObject is MonoEntityTag tag)
                window._entityTag = tag;
            else if (Selection.activeObject is VariableTag varTag)
                window._variableTag = varTag;
        }

        private static VarTagUsageWindow Open()
        {
            var window = GetWindow<VarTagUsageWindow>();
            window.titleContent = new GUIContent("Var Tag Usage");
            window.minSize = new Vector2(600, 500);
            window.Show();
            return window;
        }

        /// <summary>VariableTag Inspector 的一鍵搜尋入口：開窗並立刻掃這顆變數</summary>
        public static void OpenAndScan(VariableTag varTag)
        {
            var window = Open();
            window._variableTag = varTag;
            window.ScanVariableTag();
        }

        // ---------------- 掃描範圍 ----------------

        [Title("掃描範圍")]
        [FolderPath(RequireExistingPath = true)]
        [ListDrawerSettings(DefaultExpandedState = true)]
        public string[] _searchFolders = { "Assets/0_Gameplay", "Assets/1_Prototype" };

        public bool _scanPrefabs = true;

        [LabelText("掃已開啟的 Scene")] public bool _scanOpenScenes = true;

        [LabelText("掃資料夾下所有 Scene（慢）")] public bool _scanScenesInFolders;

        [Button("搜尋範圍設為整個 Assets")]
        private void ScanWholeAssets()
        {
            _searchFolders = new[] { "Assets" };
        }

        private ScanOptions BuildOptions()
        {
            return new ScanOptions
            {
                SearchFolders = _searchFolders,
                ScanPrefabs = _scanPrefabs,
                ScanOpenScenes = _scanOpenScenes,
                ScanScenesInFolders = _scanScenesInFolders
            };
        }

        // ---------------- 掃描入口 ----------------

        [Title("單一 VariableTag")] [AssetsOnly] [HideLabel]
        public VariableTag _variableTag;

        [Button("Scan 這顆變數", ButtonHeight = 30)]
        private void ScanVariableTag()
        {
            if (_variableTag == null)
            {
                _summary = "請先指定 VariableTag";
                Debug.LogWarning("[VarTagUsageWindow] _variableTag 沒設，中止掃描");
                return;
            }

            FillRows(VarTagUsageScanner.Scan(_variableTag, BuildOptions()));
        }

        [Title("整顆 MonoEntityTag")] [AssetsOnly] [HideLabel]
        public MonoEntityTag _entityTag;

        [Button("Scan 整顆 Entity", ButtonHeight = 30)]
        private void Scan()
        {
            if (_entityTag == null)
            {
                _summary = "請先指定 MonoEntityTag";
                Debug.LogWarning("[VarTagUsageWindow] _entityTag 沒設，中止掃描");
                return;
            }

            FillRows(VarTagUsageScanner.Scan(_entityTag, BuildOptions()));
        }

        private void FillRows(List<VarTagUsage> usages)
        {
            _rows.Clear();
            foreach (var u in usages)
                _rows.Add(new UsageRow(u));

            _summary =
                $"掃描 {VarTagUsageScanner.LastScanPrefabCount} 個 prefab、" +
                $"{VarTagUsageScanner.LastScanSceneCount} 個 scene，" +
                $"耗時 {VarTagUsageScanner.LastScanSeconds:F1}s，共 {_rows.Count} 顆變數";
        }

        [Title("結果")] [ShowInInspector] [DisplayAsString] [HideLabel]
        private string _summary = "尚未掃描";

        [Button("複製報告到剪貼簿")]
        private void CopyReport()
        {
            if (_rows.Count == 0)
            {
                Debug.LogWarning("[VarTagUsageWindow] 還沒有掃描結果，無法產生報告");
                return;
            }

            var usages = new List<VarTagUsage>(_rows.Count);
            foreach (var row in _rows)
                usages.Add(row.Usage);

            var reportTitle = _variableTag != null ? _variableTag.name :
                _entityTag != null ? _entityTag.name : "掃描結果";
            EditorGUIUtility.systemCopyBuffer = VarTagUsageScanner.BuildReport(reportTitle, usages);
            Debug.Log("[VarTagUsageWindow] 報告已複製到剪貼簿");
        }

        [TableList(AlwaysExpanded = false)] [ShowInInspector]
        private List<UsageRow> _rows = new();

        public class UsageRow
        {
            private VarTagUsage _usage;

            public UsageRow(VarTagUsage usage)
            {
                _usage = usage;
                _sourceRows = new List<SourceRow>(usage.Groups.Count);
                foreach (var g in usage.Groups)
                    _sourceRows.Add(new SourceRow(g));
            }

            public VarTagUsage Usage => _usage;

            [TableColumnWidth(220, false)]
            [ShowInInspector]
            [DisplayAsString]
            public string 變數 => _usage.TagName;

            [TableColumnWidth(50, false)]
            [ShowInInspector]
            [DisplayAsString]
            public int proxy => _usage.Proxies.Count;

            [TableColumnWidth(40, false)]
            [ShowInInspector]
            [DisplayAsString]
            public int 寫 => _usage.WriteCount;

            [TableColumnWidth(40, false)]
            [ShowInInspector]
            [DisplayAsString]
            public int 讀 => _usage.ReadCount;

            [TableColumnWidth(40, false)]
            [ShowInInspector]
            [DisplayAsString]
            public int 同步 => _usage.SyncCount;

            [TableColumnWidth(40, false)]
            [ShowInInspector]
            [DisplayAsString]
            public int 其他 => _usage.OtherCount;

            [TableColumnWidth(140, false)]
            [ShowInInspector]
            [DisplayAsString]
            public string 標記 => _usage.Warning;

            //快取在欄位裡，Inspector 每幀 get 都不重建，避免 GC
            [HideLabel] private List<SourceRow> _sourceRows;

            //get-only property 會讓 Odin 把整棵子樹判成不可編輯，裡面的 Ping Button 會被 disable
            [TableColumnWidth(360)]
            [ShowInInspector]
            [EnableGUI]
            [ListDrawerSettings(DefaultExpandedState = true, HideAddButton = true,
                HideRemoveButton = true)]
            public List<SourceRow> 來源檔案 => _sourceRows;
        }

        /// <summary>單一來源檔案（prefab 或 scene）的分組：標題含該檔案自己的 W/R/S/O 計數</summary>
        public class SourceRow
        {
            private readonly string _sourcePath;
            private List<Entry> _entries;

            public SourceRow(SourceUsageGroup group)
            {
                _sourcePath = group.SourcePath;

                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(group.CountLabel))
                    sb.Append(group.CountLabel).Append(' ');
                if (group.IsScene)
                    sb.Append("(scene) ");
                sb.Append(group.SourcePath);
                _label = sb.ToString();

                _entries = new List<Entry>(group.Proxies.Count + group.Sites.Count);
                foreach (var p in group.Proxies)
                    _entries.Add(
                        new Entry(p.SourcePath, $"proxy  {p.NodePath}  ({p.VariableType})"));
                foreach (var s in group.Sites)
                    _entries.Add(new Entry(s.SourcePath,
                        $"[{VarTagUsageScanner.KindMark(s.Kind)}]  {s.NodePath}  →  {s.ComponentType}.{s.FieldName}"));
            }

            [HideLabel] [DisplayAsString(false)] [ShowInInspector] [HorizontalGroup]
            private readonly string _label;

            [Button("Ping", ButtonHeight = 16)]
            [HorizontalGroup(Width = 50)]
            private void Ping()
            {
                PingSource(_sourcePath);
            }

            [ShowInInspector]
            [EnableGUI]
            [ListDrawerSettings(DefaultExpandedState = false, HideAddButton = true,
                HideRemoveButton = true)]
            [LabelText("明細")]
            public List<Entry> Entries => _entries;
        }

        public class Entry
        {
            private readonly string _sourcePath;

            public Entry(string sourcePath, string label)
            {
                _sourcePath = sourcePath;
                _label = label;
            }

            [HideLabel] [DisplayAsString(false)] [ShowInInspector]
            private readonly string _label;

            [Button("Ping", ButtonHeight = 16)]
            [HorizontalGroup]
            private void Ping()
            {
                PingSource(_sourcePath);
            }
        }

        /// <summary>prefab 與 scene 共用的 Ping（SceneAsset 不是 GameObject，要用 Object 載）</summary>
        private static void PingSource(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogWarning("[VarTagUsageWindow] 這筆沒有資產路徑（可能是未存檔的 scene），無法 Ping");
                return;
            }

            var obj = AssetDatabase.LoadAssetAtPath<Object>(sourcePath);
            if (obj == null)
            {
                Debug.LogWarning($"[VarTagUsageWindow] 載不到資產: {sourcePath}");
                return;
            }

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }
    }
}
