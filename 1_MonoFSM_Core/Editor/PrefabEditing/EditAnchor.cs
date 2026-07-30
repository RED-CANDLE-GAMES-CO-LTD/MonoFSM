using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 離線索引的 anchor（`Assets/…/X.prefab#&lt;fileID&gt;`）→ 合併後、可直接餵給
    /// `--node` 的完整路徑。
    ///
    /// 存在的理由：`up find` 走離線 YAML，回傳的節點路徑是**局部的** —— Unity 只在
    /// 「本檔有東西引用到」時才寫出 stripped 佔位 document，variant 繼承來的父節點在本檔
    /// 根本查不到，所以那條路徑前面常常缺一段，同層同名節點也沒有 `[n]` 索引。直接餵給
    /// `prefab read --node` 會失敗或指錯節點。
    ///
    /// 這裡走 Unity 端的**合併後**物件圖（跟 `prefab read` 同一份 `LoadAssetAtPath`），
    /// 所以拿到的路徑跟讀取端看到的一定是同一套。
    ///
    /// 難點是 anchor 的 fileID 反查：variant 裡繼承來的節點，其 fileID 是合成出來的，
    /// 演算法不公開。所以分層比對（同 guid 的 localId → 任意 guid 的 localId →
    /// prefab source 鏈上的 localId → 名稱唯一），**每一層都是確定性比對，比不到就回報
    /// 失敗，不猜**。
    /// </summary>
    public static class EditAnchor
    {
        /// <summary>
        /// 批次解析。輸入一行一個 `assetPath#fileID[|節點名]`（節點名可省，只用在最後一層
        /// fallback 與錯誤診斷）。輸出一行一個 `anchor\tok\t路徑` 或 `anchor\tfail\t原因`。
        ///
        /// 批次是刻意的：`find` 一次可能命中幾十筆，逐筆呼叫 Unity 會慢到不能用，
        /// 而同一份 asset 的反查表建一次就能重複用。
        /// </summary>
        public static string Resolve(string anchors)
        {
            var sb = new StringBuilder();
            var groups = new Dictionary<string, List<(string anchor, long id, string name)>>();
            var order = new List<string>();

            foreach (var raw in (anchors ?? "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                var bar = line.IndexOf('|');
                var nodeName = bar < 0 ? null : line.Substring(bar + 1);
                var anchor = bar < 0 ? line : line.Substring(0, bar);

                var hash = anchor.LastIndexOf('#');
                if (hash <= 0 || !long.TryParse(anchor.Substring(hash + 1), out var fileId))
                {
                    sb.AppendLine($"{anchor}\tfail\tanchor 格式不對，要 assetPath#fileID");
                    continue;
                }

                var assetPath = anchor.Substring(0, hash);
                if (!groups.TryGetValue(assetPath, out var list))
                {
                    groups[assetPath] = list = new List<(string, long, string)>();
                    order.Add(assetPath);
                }

                list.Add((anchor, fileId, nodeName));
            }

            foreach (var assetPath in order)
                ResolveAsset(assetPath, groups[assetPath], sb);

            return sb.ToString();
        }

        private static void ResolveAsset(
            string assetPath, List<(string anchor, long id, string name)> items, StringBuilder sb)
        {
            var isScene = assetPath.EndsWith(".unity");
            List<Transform> roots;

            if (isScene)
            {
                var scene = SceneManager.GetSceneByPath(assetPath);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Fail(items, sb,
                        $"scene 沒開著（anchor 在 scene 裡只有那個 scene 開著才解得開）。" +
                        $"先 up scene open \"{assetPath}\"");
                    return;
                }

                roots = scene.GetRootGameObjects().Select(g => g.transform).ToList();
            }
            else
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset == null)
                {
                    Fail(items, sb, $"載不到 prefab（路徑不存在，或不是 GameObject 資產）：{assetPath}");
                    return;
                }

                roots = new List<Transform> { asset.transform };
            }

            var table = new AnchorTable(assetPath, roots);
            foreach (var (anchor, id, name) in items)
            {
                var node = table.Lookup(id, name, out var how, out var reason);
                if (node == null)
                {
                    sb.AppendLine($"{anchor}\tfail\t{reason}");
                    continue;
                }

                var path = table.PathOf(node);
                if (path == null)
                {
                    sb.AppendLine($"{anchor}\tfail\t找到節點 '{node.name}' 但算不出它到 root 的路徑");
                    continue;
                }

                sb.AppendLine($"{anchor}\tok\t{path}\t{how}");
            }
        }

        private static void Fail(
            IEnumerable<(string anchor, long id, string name)> items, StringBuilder sb, string reason)
        {
            foreach (var (anchor, _, _) in items) sb.AppendLine($"{anchor}\tfail\t{reason}");
        }

        /// <summary>
        /// 一份 asset 的反查表。三張 map 對應三種 fileID 來源，優先序由確定性高到低。
        /// </summary>
        private class AnchorTable
        {
            private readonly bool _isScene;
            private readonly List<Transform> _roots;

            /// <summary>localId（且 guid 就是這份 asset）→ 節點。本檔自己寫出的節點走這條。</summary>
            private readonly Dictionary<long, Transform> _own = new();

            /// <summary>localId（guid 是別份 asset）→ 節點。多層 variant 有機會落在這裡。</summary>
            private readonly Dictionary<long, Transform> _foreign = new();

            /// <summary>prefab source 鏈上任一層的 localId → 節點。繼承來的節點主要靠這條。</summary>
            private readonly Dictionary<long, Transform> _source = new();

            /// <summary>撞號的 id 直接標成不可用 —— 猜錯比解不開更糟。</summary>
            private readonly HashSet<long> _ambiguous = new();

            private readonly Dictionary<string, List<Transform>> _byName = new();

            public AnchorTable(string assetPath, List<Transform> roots)
            {
                _isScene = assetPath.EndsWith(".unity");
                _roots = roots;
                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                var all = new List<Transform>();

                foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    all.Add(t);
                    if (!_byName.TryGetValue(t.name, out var list))
                        _byName[t.name] = list = new List<Transform>();
                    list.Add(t);

                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                            t.gameObject, out var guid, out long id) && id != 0)
                        Add(guid == assetGuid ? _own : _foreign, id, t);

                    // variant 繼承來的節點：往上追 source 鏈，把每一層的 localId 都記下來。
                    // anchor 的 fileID 可能是任一層寫出來的（stripped document 記的就是來源那層）。
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    var hops = 0;
                    while (src != null && hops++ < 8)
                    {
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                                src, out _, out long srcId) && srcId != 0)
                            Add(_source, srcId, t);
                        src = PrefabUtility.GetCorrespondingObjectFromSource(src);
                    }
                }

                if (_isScene) AddSceneIds(all, assetGuid);
            }

            /// <summary>
            /// scene 物件的 `TryGetGUIDAndLocalFileIdentifier` 拿不到 scene YAML 裡的 fileID
            /// （實測回 0 / 對不上），所以 scene 另外走 GlobalObjectId：
            /// 非 prefab instance 的 scene 物件，`targetObjectId` 就是 YAML 裡的那個 fileID。
            /// 用批次版 —— 逐個 `GetGlobalObjectIdSlow` 在幾千節點的 scene 上會慢到不能用。
            /// </summary>
            private void AddSceneIds(List<Transform> all, string assetGuid)
            {
                var objs = all.Select(t => (Object)t.gameObject).ToArray();
                var ids = new GlobalObjectId[objs.Length];
                GlobalObjectId.GetGlobalObjectIdsSlow(objs, ids);

                for (var i = 0; i < objs.Length; i++)
                {
                    var id = ids[i];
                    var target = unchecked((long)id.targetObjectId);
                    if (target == 0) continue;

                    // prefab instance 底下的物件：targetObjectId 是**來源 prefab** 裡的 id，
                    // scene YAML 記的是合成 id，所以只能當次要線索。
                    var map = id.targetPrefabId != 0
                        ? _source
                        : id.assetGUID.ToString() == assetGuid
                            ? _own
                            : _foreign;
                    Add(map, target, all[i]);
                }
            }

            private void Add(Dictionary<long, Transform> map, long id, Transform t)
            {
                if (map.TryGetValue(id, out var existing))
                {
                    if (existing != t) _ambiguous.Add(id);
                    return;
                }

                map[id] = t;
            }

            public Transform Lookup(long id, string name, out string how, out string reason)
            {
                how = null;
                reason = null;

                if (_ambiguous.Contains(id))
                {
                    reason = $"fileID {id} 在合併後對到多個節點，無法確定是哪一個";
                    return null;
                }

                if (_own.TryGetValue(id, out var t)) { how = "own"; return t; }
                if (_foreign.TryGetValue(id, out t)) { how = "foreign"; return t; }
                if (_source.TryGetValue(id, out t)) { how = "inherited"; return t; }

                if (!string.IsNullOrEmpty(name) &&
                    _byName.TryGetValue(name, out var sameName) && sameName.Count == 1)
                {
                    how = "by-name";
                    return sameName[0];
                }

                var dup = !string.IsNullOrEmpty(name) && _byName.TryGetValue(name, out var many)
                    ? many.Count
                    : 0;
                reason =
                    $"合併後的物件圖裡找不到 fileID {id}" +
                    (string.IsNullOrEmpty(name)
                        ? "（沒給節點名，名稱 fallback 用不上）"
                        : dup == 0
                            ? $"，也沒有叫 '{name}' 的節點（節點可能已刪除，或索引過期，先 up index）"
                            : $"，且叫 '{name}' 的節點有 {dup} 個，無法用名稱唯一決定");
                return null;
            }

            /// <summary>
            /// scene 的第一段要是 root object 名稱（`scene ls --node` 的語彙），
            /// prefab 則不含 root（`prefab read --node` 的語彙）。
            /// </summary>
            public string PathOf(Transform node)
            {
                for (var i = 0; i < _roots.Count; i++)
                {
                    var root = _roots[i];
                    var rel = EditResolve.PathOf(root, node);
                    if (rel == null) continue;
                    if (!_isScene) return rel;

                    var head = RootSegment(i);
                    return rel.Length == 0 ? head : $"{head}/{rel}";
                }

                return null;
            }

            /// <summary>scene 的 root 也可能整排同名（十幾個 AppCallbackListener），一樣要標 `[n]`。</summary>
            private string RootSegment(int index)
            {
                var name = _roots[index].name;
                var n = 0;
                var dup = false;
                for (var i = 0; i < _roots.Count; i++)
                {
                    if (i == index || _roots[i].name != name) continue;
                    dup = true;
                    if (i < index) n++;
                }

                return dup ? $"{name}[{n}]" : name;
            }
        }
    }
}
