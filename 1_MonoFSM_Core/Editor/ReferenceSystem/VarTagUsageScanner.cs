using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace MonoFSM.Editor.ReferenceSystem
{
    /// <summary>掃描範圍設定</summary>
    public class ScanOptions
    {
        /// <summary>限定搜尋資料夾；null 或空 = 整個 Assets</summary>
        public string[] SearchFolders;

        public bool ScanPrefabs = true;

        /// <summary>掃目前 Editor 已開啟的 scene（幾乎不花時間）</summary>
        public bool ScanOpenScenes = true;

        /// <summary>把 SearchFolders 底下的 scene 檔逐一用 preview scene 開起來掃（慢）</summary>
        public bool ScanScenesInFolders = false;
    }

    /// <summary>
    ///     反查「哪些 prefab / scene / FSM 動到某個 MonoEntityTag 底下的變數」，並分辨讀 / 寫。
    ///     proxy VarBool 身上只有 _varTag，不直接引用真身，所以 up refs / grep fileID 都查不到；
    ///     離線索引對 nested prefab instance 的階層是斷的、對 m_Modifications 型 override 也查不到，
    ///     所以這個工具必須走 Unity 端，靠 Unity 解析好的 prefab 階層。
    ///     已知限制：只找「靜態序列化的引用」。執行期用 GetVar(tag) 動態取值的地方查不到。
    /// </summary>
    public static class VarTagUsageScanner
    {
        /// <summary>上一次掃描掃過的 prefab 數量</summary>
        public static int LastScanPrefabCount { get; private set; }

        /// <summary>上一次掃描掃過的 scene 數量</summary>
        public static int LastScanSceneCount { get; private set; }

        /// <summary>上一次掃描耗時（秒）</summary>
        public static float LastScanSeconds { get; private set; }

        /// <summary>掃一顆 entity tag 底下宣告的所有變數</summary>
        public static List<VarTagUsage> Scan(MonoEntityTag entityTag, ScanOptions options = null)
        {
            if (entityTag == null)
            {
                Debug.LogWarning("[VarTagUsageScanner] entityTag is null");
                return new List<VarTagUsage>();
            }

            return ScanTags(entityTag.containsVariableTypeTags, entityTag.name, options);
        }

        /// <summary>只掃單一顆變數 tag（不需要它掛在哪顆 MonoEntityTag 底下）</summary>
        public static List<VarTagUsage> Scan(VariableTag varTag, ScanOptions options = null)
        {
            if (varTag == null)
            {
                Debug.LogWarning("[VarTagUsageScanner] varTag is null");
                return new List<VarTagUsage>();
            }

            return ScanTags(new[] { varTag }, varTag.name, options);
        }

        /// <param name="label">只用在進度條與警告訊息上</param>
        public static List<VarTagUsage> ScanTags(IEnumerable<VariableTag> tags, string label,
            ScanOptions options = null)
        {
            options ??= new ScanOptions();

            var result = new List<VarTagUsage>();
            var sw = Stopwatch.StartNew();

            var usageByTag = new Dictionary<VariableTag, VarTagUsage>();
            if (tags != null)
                foreach (var tag in tags)
                {
                    if (tag == null)
                        continue;
                    if (usageByTag.ContainsKey(tag))
                        continue;
                    var usage = new VarTagUsage { Tag = tag };
                    usageByTag.Add(tag, usage);
                    result.Add(usage);
                }

            LastScanPrefabCount = 0;
            LastScanSceneCount = 0;

            if (usageByTag.Count == 0)
            {
                Debug.LogWarning($"[VarTagUsageScanner] {label} 沒有任何要掃的變數 tag");
                sw.Stop();
                LastScanSeconds = (float)sw.Elapsed.TotalSeconds;
                return result;
            }

            var folders = ValidateFolders(options.SearchFolders);

            //全域 proxy 對照表：變數實體 → 它宣告的 tag（跨 prefab / scene 引用也查得到）
            var proxyToTag = new Dictionary<AbstractMonoVariable, VariableTag>();
            //單一 root 用的暫存陣列，避免每個 prefab 都 new 一次
            var rootBuffer = new GameObject[1];

            try
            {
                //順序必須是 prefab 先、scene 後：scene 上的引用可能指向 prefab 資產裡的 proxy，
                //反之 prefab 不可能引用 scene 物件
                if (options.ScanPrefabs)
                    ScanPrefabs(folders, proxyToTag, usageByTag, rootBuffer);

                var scannedScenePaths = new HashSet<string>(StringComparer.Ordinal);
                if (options.ScanOpenScenes)
                    ScanOpenScenes(proxyToTag, usageByTag, scannedScenePaths);
                if (options.ScanScenesInFolders)
                    ScanScenesInFolders(folders, proxyToTag, usageByTag, scannedScenePaths);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (var usage in result)
                usage.RecountFromSites();

            sw.Stop();
            LastScanSeconds = (float)sw.Elapsed.TotalSeconds;
            return result;
        }

        /// <summary>濾掉空字串與不存在的資料夾；沒有任何有效資料夾就回 null（= 整個 Assets）</summary>
        private static string[] ValidateFolders(string[] searchFolders)
        {
            if (searchFolders == null || searchFolders.Length == 0)
                return null;

            var valid = new List<string>(searchFolders.Length);
            var dropped = new List<string>();
            foreach (var f in searchFolders)
            {
                if (string.IsNullOrWhiteSpace(f))
                    continue;
                var folder = f.TrimEnd('/');
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    dropped.Add(f);
                    continue;
                }

                if (!valid.Contains(folder))
                    valid.Add(folder);
            }

            if (dropped.Count > 0)
                Debug.LogWarning(
                    $"[VarTagUsageScanner] 以下搜尋資料夾不存在，已略過：{string.Join(", ", dropped)}");

            if (valid.Count == 0)
            {
                Debug.LogWarning("[VarTagUsageScanner] 沒有任何有效的搜尋資料夾，改掃整個 Assets");
                return null;
            }

            return valid.ToArray();
        }

        private static string[] FindAssets(string filter, string[] folders)
        {
            return folders == null || folders.Length == 0
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, folders);
        }

        // ---------------- prefab ----------------

        private static void ScanPrefabs(string[] folders,
            Dictionary<AbstractMonoVariable, VariableTag> proxyToTag,
            Dictionary<VariableTag, VarTagUsage> usageByTag,
            GameObject[] rootBuffer)
        {
            var guids = FindAssets("t:Prefab", folders);
            LastScanPrefabCount = guids.Length;

            //stage1 命中的 prefab，stage2 只對這些找引用
            var hitPrefabs = new List<(string path, GameObject root)>();

            // ---- 階段 1：找 proxy ----
            // 用 LoadAssetAtPath（唯讀、階層完整），不要用 PrefabUtility.LoadPrefabContents（會 instantiate，全庫會慢到不能用）
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;

                if ((i & 63) == 0 &&
                    EditorUtility.DisplayCancelableProgressBar("Var Tag Usage Scan",
                        $"階段 1 找 prefab proxy… {i}/{guids.Length}", (float)i / guids.Length))
                    break;

                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                    continue;

                rootBuffer[0] = root;
                if (CollectProxies(path, false, rootBuffer, proxyToTag, usageByTag))
                    hitPrefabs.Add((path, root));
            }

            // ---- 階段 2：找誰指向那些 proxy ----
            for (var i = 0; i < hitPrefabs.Count; i++)
            {
                var (path, root) = hitPrefabs[i];
                if ((i & 15) == 0 &&
                    EditorUtility.DisplayCancelableProgressBar("Var Tag Usage Scan",
                        $"階段 2 找 prefab 引用… {i}/{hitPrefabs.Count}", (float)i / hitPrefabs.Count))
                    break;

                rootBuffer[0] = root;
                CollectUsageSites(path, false, rootBuffer, proxyToTag, usageByTag);
            }
        }

        // ---------------- scene ----------------

        /// <summary>掃目前 Editor 已開啟的 scene（不會動到使用者的 scene，只讀）</summary>
        private static void ScanOpenScenes(
            Dictionary<AbstractMonoVariable, VariableTag> proxyToTag,
            Dictionary<VariableTag, VarTagUsage> usageByTag,
            HashSet<string> scannedScenePaths)
        {
            var count = SceneManager.sceneCount;
            for (var i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                //未存檔的 scene path 會是空字串，用 name 當顯示用來源（Ping 不到）
                var sourcePath = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
                if (!string.IsNullOrEmpty(scene.path))
                    scannedScenePaths.Add(scene.path);

                var roots = scene.GetRootGameObjects();
                //preview scene 一關物件就沒了，所以 scene 不能分兩階段：同一趟先收 proxy 再找引用
                CollectProxies(sourcePath, true, roots, proxyToTag, usageByTag);
                CollectUsageSites(sourcePath, true, roots, proxyToTag, usageByTag);
                LastScanSceneCount++;
            }
        }

        /// <summary>把資料夾底下的 scene 檔逐一用 preview scene 開起來掃（慢），絕不動使用者當前開啟的 scene</summary>
        private static void ScanScenesInFolders(string[] folders,
            Dictionary<AbstractMonoVariable, VariableTag> proxyToTag,
            Dictionary<VariableTag, VarTagUsage> usageByTag,
            HashSet<string> scannedScenePaths)
        {
            var guids = FindAssets("t:Scene", folders);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (!scannedScenePaths.Add(path)) //已經在 Editor 開著掃過了，別重複計數
                    continue;

                if (EditorUtility.DisplayCancelableProgressBar("Var Tag Usage Scan",
                        $"掃 scene… {i}/{guids.Length}  {path}", (float)i / guids.Length))
                    break;

                var scene = default(Scene);
                try
                {
                    scene = EditorSceneManager.OpenPreviewScene(path);
                    if (!scene.IsValid())
                    {
                        Debug.LogWarning($"[VarTagUsageScanner] preview scene 開不起來，略過：{path}");
                        continue;
                    }

                    var roots = scene.GetRootGameObjects();
                    CollectProxies(path, true, roots, proxyToTag, usageByTag);
                    CollectUsageSites(path, true, roots, proxyToTag, usageByTag);
                    LastScanSceneCount++;
                }
                finally
                {
                    if (scene.IsValid())
                        EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        // ---------------- 共用走訪 ----------------

        /// <summary>回傳這批 roots 裡有沒有命中的 proxy</summary>
        private static bool CollectProxies(string sourcePath, bool isScene, GameObject[] roots,
            Dictionary<AbstractMonoVariable, VariableTag> proxyToTag,
            Dictionary<VariableTag, VarTagUsage> usageByTag)
        {
            var hit = false;
            if (roots == null)
                return false;

            foreach (var root in roots)
            {
                if (root == null)
                    continue;

                var vars = root.GetComponentsInChildren<AbstractMonoVariable>(true);
                if (vars == null || vars.Length == 0)
                    continue;

                foreach (var v in vars)
                {
                    if (v == null || v._varTag == null)
                        continue;
                    if (!usageByTag.TryGetValue(v._varTag, out var usage))
                        continue;

                    //即使 proxy 是繼承來的，也要登記進 proxyToTag，
                    //因為這個檔案內部（含 override）可能有自己的引用要掃到
                    hit = true;
                    proxyToTag[v] = v._varTag;

                    //繼承自 base / nested prefab 且 _varTag 沒被 override → 由來源 prefab 負責計數
                    if (IsInheritedNotOverridden(v, nameof(AbstractMonoVariable._varTag)))
                        continue;

                    usage.Proxies.Add(new VarProxySite
                    {
                        SourcePath = sourcePath,
                        IsScene = isScene,
                        NodePath = GetNodePath(v.transform, root.transform),
                        VariableType = v.GetType().Name
                    });
                }
            }

            return hit;
        }

        private static void CollectUsageSites(string sourcePath, bool isScene, GameObject[] roots,
            Dictionary<AbstractMonoVariable, VariableTag> proxyToTag,
            Dictionary<VariableTag, VarTagUsage> usageByTag)
        {
            if (roots == null)
                return;

            foreach (var root in roots)
            {
                if (root == null)
                    continue;

                var comps = root.GetComponentsInChildren<Component>(true);
                foreach (var comp in comps)
                {
                    if (comp == null) //missing script
                        continue;

                    //這顆 component 是不是從 base / nested prefab 繼承來的
                    var isInherited = PrefabUtility.GetCorrespondingObjectFromSource(comp) != null;

                    var so = new SerializedObject(comp);
                    var it = so.GetIterator();
                    //不碰 isArray 判斷（isArray 對 string 也回 true），直接 Next(true) 走遍全部
                    while (it.Next(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference)
                            continue;
                        //繼承來的 component 且該欄位沒被 override → 由來源 prefab 負責計數，避免重複
                        if (isInherited && !it.prefabOverride)
                            continue;
                        //instanceID 只是 int 不會 throw，先擋掉沒指東西的欄位（絕大多數）
                        if (it.objectReferenceInstanceIDValue == 0)
                            continue;

                        //壞掉的 PPtr（序列化的型別與欄位宣告型別對不上）會讓 objectReferenceValue
                        //丟 InvalidCastException，不能讓一顆壞欄位中斷整輪掃描
                        UnityEngine.Object refObj;
                        try
                        {
                            refObj = it.objectReferenceValue;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("[VarTagUsageScanner] 引用讀取失敗，已略過：" +
                                             $"{sourcePath} → {comp.GetType().Name}.{it.propertyPath}（{e.GetType().Name}）",
                                comp);
                            continue;
                        }

                        if (refObj is not AbstractMonoVariable v)
                            continue;
                        if (ReferenceEquals(v, comp)) //自己指自己，沒意義
                            continue;
                        if (!proxyToTag.TryGetValue(v, out var tag))
                            continue;
                        if (!usageByTag.TryGetValue(tag, out var usage))
                            continue;

                        var fieldName = NormalizeFieldName(it.propertyPath);
                        usage.Sites.Add(new VarUsageSite
                        {
                            SourcePath = sourcePath,
                            IsScene = isScene,
                            NodePath = GetNodePath(comp.transform, root.transform),
                            ComponentType = comp.GetType().Name,
                            FieldName = fieldName,
                            Kind = Classify(comp, fieldName)
                        });
                    }

                    so.Dispose();
                }
            }
        }

        /// <summary>
        ///     component 是繼承自 base / nested prefab，且指定欄位在這顆 prefab 沒被 override。
        ///     這種情況該引用應由來源 prefab 負責計數，否則同一份邏輯會在每個 variant 各數一次。
        /// </summary>
        private static bool IsInheritedNotOverridden(Component comp, string fieldName)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(comp) == null)
                return false;

            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            var overridden = prop != null && prop.prefabOverride;
            so.Dispose();
            return !overridden;
        }

        /// <summary>
        ///     寫入目標欄位沒有統一命名（_target / _targetVar / _objectVariable…），
        ///     所以規則反過來：action 上帶 "source" 的欄位是讀，其餘指向 Var 的欄位視為寫。
        /// </summary>
        private static VarUsageKind Classify(Component comp, string fieldName)
        {
            if (comp is AbstractStateAction)
                return !string.IsNullOrEmpty(fieldName) &&
                       fieldName.IndexOf("source", StringComparison.OrdinalIgnoreCase) >= 0
                    ? VarUsageKind.Read
                    : VarUsageKind.Write;
            if (comp is AbstractConditionBehaviour)
                return VarUsageKind.Read;
            if (comp.GetType().Name.Contains("NetworkedVarSync"))
                return VarUsageKind.Sync;
            return VarUsageKind.Other;
        }

        /// <summary>
        ///     取 propertyPath 的第一段："_targets.Array.data[3]" → "_targets"，
        ///     "_source1Var._varFloat"（wrapper 內部欄位）→ "_source1Var"。
        ///     只取末段的話 wrapper 會退化成 "_varFloat"，"source" 判定就失效。
        /// </summary>
        private static string NormalizeFieldName(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return propertyPath;
            var path = propertyPath;
            var dot = path.IndexOf('.');
            if (dot >= 0)
                path = path.Substring(0, dot);
            var bracket = path.IndexOf('[');
            if (bracket >= 0)
                path = path.Substring(0, bracket);
            return path;
        }

        private static string GetNodePath(Transform t, Transform root)
        {
            if (t == null)
                return string.Empty;
            if (t == root)
                return t.name;

            var sb = new StringBuilder(t.name);
            var p = t.parent;
            while (p != null && p != root)
            {
                sb.Insert(0, '/').Insert(0, p.name);
                p = p.parent;
            }

            return sb.ToString();
        }

        // ---------------- 報告 ----------------

        /// <summary>給 CLI 用：一行 return VarTagUsageScanner.ScanReport("d_TeamStatus"); 就拿到結果</summary>
        public static string ScanReport(string entityTagAssetName, ScanOptions options = null)
        {
            if (string.IsNullOrEmpty(entityTagAssetName))
                return "[VarTagUsageScanner] entityTagAssetName 是空的";

            var guids = AssetDatabase.FindAssets($"{entityTagAssetName} t:MonoEntityTag");
            if (guids == null || guids.Length == 0)
                return $"[VarTagUsageScanner] 找不到 MonoEntityTag: {entityTagAssetName}";

            MonoEntityTag entityTag = null;
            var candidates = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tag = AssetDatabase.LoadAssetAtPath<MonoEntityTag>(path);
                if (tag == null)
                    continue;
                candidates.Add(path);
                if (tag.name == entityTagAssetName && entityTag == null)
                    entityTag = tag;
            }

            if (entityTag == null)
            {
                if (candidates.Count == 0)
                    return $"[VarTagUsageScanner] 找不到 MonoEntityTag: {entityTagAssetName}";
                return $"[VarTagUsageScanner] 沒有名稱剛好等於 {entityTagAssetName} 的 MonoEntityTag，" +
                       $"相近的有：\n{string.Join("\n", candidates)}";
            }

            var usages = Scan(entityTag, options);
            return BuildReport(entityTag.name, usages);
        }

        /// <summary>給 CLI 用：單一 VariableTag 的引用報告</summary>
        public static string ScanTagReport(string varTagAssetName, ScanOptions options = null)
        {
            if (string.IsNullOrEmpty(varTagAssetName))
                return "[VarTagUsageScanner] varTagAssetName 是空的";

            var guids = AssetDatabase.FindAssets($"{varTagAssetName} t:VariableTag");
            VariableTag varTag = null;
            var candidates = new List<string>();
            if (guids != null)
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var tag = AssetDatabase.LoadAssetAtPath<VariableTag>(path);
                    if (tag == null)
                        continue;
                    candidates.Add(path);
                    if (tag.name == varTagAssetName && varTag == null)
                        varTag = tag;
                }

            if (varTag == null)
            {
                if (candidates.Count == 0)
                    return $"[VarTagUsageScanner] 找不到 VariableTag: {varTagAssetName}";
                return $"[VarTagUsageScanner] 沒有名稱剛好等於 {varTagAssetName} 的 VariableTag，" +
                       $"相近的有：\n{string.Join("\n", candidates)}";
            }

            return BuildReport(varTag.name, Scan(varTag, options));
        }

        public static string BuildReport(string title, List<VarTagUsage> usages)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"{title} 變數交叉引用（掃描 {LastScanPrefabCount} 個 prefab、{LastScanSceneCount} 個 scene，耗時 {LastScanSeconds:F1}s）");
            const string line = "─────────────────────────────────────────────";
            sb.AppendLine(line);

            //欄位寬度（CJK 算兩格）
            var nameWidth = 8;
            foreach (var u in usages)
                nameWidth = Mathf.Max(nameWidth, DisplayWidth(u.TagName));
            nameWidth = Mathf.Min(nameWidth, 40);

            sb.Append(Pad("變數", nameWidth)).AppendLine("  proxy   寫   讀  同步  其他");
            foreach (var u in usages)
            {
                sb.Append(Pad(u.TagName, nameWidth));
                sb.Append($"  {u.Proxies.Count,5} {u.WriteCount,3} {u.ReadCount,3} {u.SyncCount,4} {u.OtherCount,5}");
                var warn = u.Warning;
                if (!string.IsNullOrEmpty(warn))
                    sb.Append("   ").Append(warn);
                sb.AppendLine();
            }

            sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine("## 明細");
            foreach (var u in usages)
            {
                if (u.Proxies.Count == 0 && u.Sites.Count == 0)
                    continue;

                sb.Append("### ").Append(Pad(u.TagName, nameWidth));
                sb.AppendLine(
                    $"  proxy {u.Proxies.Count}   W{u.WriteCount} R{u.ReadCount} S{u.SyncCount} O{u.OtherCount}");

                foreach (var g in u.Groups)
                {
                    sb.Append("  ");
                    if (g.IsScene)
                        sb.Append("(scene) ");
                    sb.Append(g.SourcePath);
                    if (!string.IsNullOrEmpty(g.CountLabel))
                        sb.Append("        ").Append(g.CountLabel);
                    sb.AppendLine();

                    foreach (var p in g.Proxies)
                        sb.AppendLine($"    proxy  {p.NodePath}  ({p.VariableType})");
                    foreach (var s in g.Sites)
                        sb.AppendLine(
                            $"    [{KindMark(s.Kind)}]  {s.NodePath}  →  {s.ComponentType}.{s.FieldName}");
                }
            }

            return sb.ToString();
        }

        public static string KindMark(VarUsageKind kind)
        {
            switch (kind)
            {
                case VarUsageKind.Write: return "W";
                case VarUsageKind.Read: return "R";
                case VarUsageKind.Sync: return "S";
                default: return "O";
            }
        }

        private static int DisplayWidth(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;
            var w = 0;
            foreach (var c in s)
                w += c > 0x2E7F ? 2 : 1;
            return w;
        }

        private static string Pad(string s, int width)
        {
            var w = DisplayWidth(s);
            if (w >= width)
                return s;
            return s + new string(' ', width - w);
        }
    }
}
