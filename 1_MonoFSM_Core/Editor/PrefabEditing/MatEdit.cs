#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 把 .mat 改成 Material Variant（或解除），供 uprefab CLI（`up mat set-parent`）呼叫。
    ///
    /// 為什麼走 Material.parent API 不離線改 YAML：variant 的 .mat 只存「跟 parent 不同的屬性」，
    /// 哪些算 override 由 Unity 判斷，keyword / renderQueue / disabledShaderPasses 也各自有繼承規則；
    /// 離線把 m_Parent 塞進去等於讓 child 沿用舊的完整屬性表，Unity 下次存檔時會怎麼收斂不可預期。
    ///
    /// 保證「合併後的值不變」：設 parent 前把 shader 每個 property、keyword、renderQueue、
    /// instancing / GI 旗標、關掉的 pass、property lock 全部快照，設完以後跟 parent 相同的就
    /// Revert（不留多餘 override）、不同的寫回（變成 override），最後再快照一次比對，有差異逐項列出。
    /// 失敗不 throw，回傳 `# 未修改：原因`（比照 AssetEdit）。
    /// </summary>
    public static class MatEdit
    {
        /// <summary>
        /// childPaths 以換行分隔；parentPath = "none" 代表解除 variant。
        /// parent 不存在且 createFrom 有值時，從 createFrom 複製一份（新 guid）當 parent。
        /// shader 跟 parent 不同的 child 預設跳過（force=true 才硬設，shader 會變成 parent 的）。
        /// </summary>
        public static string SetParent(string childPaths, string parentPath, string createFrom = null,
            bool force = false)
        {
            try
            {
                return SetParentInner(childPaths, parentPath, createFrom, force);
            }
            catch (Abort abort)
            {
                return $"# 未修改：{abort.Message}";
            }
        }

        private static string SetParentInner(string childPaths, string parentPath, string createFrom, bool force)
        {
            var sb = new StringBuilder();
            var paths = (childPaths ?? "").Split('\n').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            if (paths.Count == 0)
                throw new Abort("沒給 child .mat");

            // 先把所有 child 解析完，任何一顆找不到就整批不動
            var children = new List<(string path, Material mat)>();
            foreach (var p in paths)
                children.Add((p, LoadMat(p, "child")));

            Material parent = null;
            var unparent = string.IsNullOrEmpty(parentPath) || parentPath.Trim().ToLowerInvariant() == "none";
            if (!unparent)
            {
                parent = AssetDatabase.LoadAssetAtPath<Material>(parentPath);
                if (parent == null)
                {
                    if (string.IsNullOrEmpty(createFrom))
                        throw new Abort($"parent 不存在：{parentPath}{Candidates(parentPath)}\n" +
                                        "  要新建就加 --create-from <src.mat>（從 src 複製一份當 parent）");
                    var src = LoadMat(createFrom, "--create-from");
                    if (!parentPath.EndsWith(".mat"))
                        throw new Abort($"parent 路徑要以 .mat 結尾：{parentPath}");
                    var dir = Path.GetDirectoryName(parentPath)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                        throw new Abort($"parent 的資料夾不存在（或 Unity 還沒 import）：{dir}");
                    if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(src), parentPath))
                        throw new Abort($"CopyAsset 失敗：{AssetDatabase.GetAssetPath(src)} → {parentPath}");
                    parent = AssetDatabase.LoadAssetAtPath<Material>(parentPath);
                    if (parent == null)
                        throw new Abort($"複製完載不到 {parentPath}");
                    sb.AppendLine($"建立 parent {parentPath}（複製自 {AssetDatabase.GetAssetPath(src)}，" +
                                  $"guid {AssetDatabase.AssetPathToGUID(parentPath)}）");
                }
                else if (!string.IsNullOrEmpty(createFrom))
                {
                    sb.AppendLine($"# parent 已存在，忽略 --create-from（沒有覆蓋 {parentPath}）");
                }
            }

            var changed = 0;
            foreach (var (path, child) in children)
            {
                var name = Path.GetFileName(path);
                if (parent != null && child == parent)
                {
                    sb.AppendLine($"- {name}: 跳過（就是 parent 本身）");
                    continue;
                }

                if (parent != null && IsAncestorOrSelf(child, parent))
                {
                    sb.AppendLine($"- {name}: 跳過（它是 parent 的祖先，設下去會循環）");
                    continue;
                }

                if (parent != null && child.shader != parent.shader && !force)
                {
                    sb.AppendLine($"- {name}: ⚠ 跳過，shader 不同（{ShaderName(child)} vs parent {ShaderName(parent)}）。" +
                                  "variant 會吃 parent 的 shader，確定要換就加 --force");
                    continue;
                }

                if (child.parent == parent)
                {
                    sb.AppendLine($"- {name}: 不用改（parent 已經是 {(parent ? parent.name : "none")}）");
                    continue;
                }

                var before = Snap.Take(child);
                Undo.RecordObject(child, "up mat set-parent");
                child.parent = parent;
                var afterSetter = Snap.Take(child);
                var drifted = before.Diff(afterSetter).Count;

                Restore(child, parent, before);
                EditorUtility.SetDirty(child);

                var after = Snap.Take(child);
                var diffs = before.Diff(after);
                var overrides = parent == null ? 0 : CountOverrides(child);
                changed++;

                sb.Append($"- {name}: parent → {(parent ? parent.name : "none")}");
                if (parent != null)
                    sb.Append($"，override {overrides} 個屬性");
                sb.Append($"（setter 後有 {drifted} 項值跑掉，已寫回）");
                if (diffs.Count == 0)
                    sb.AppendLine("  ✓ 合併後值跟改之前一致");
                else
                {
                    sb.AppendLine($"  ⚠ 還有 {diffs.Count} 項不一致：");
                    foreach (var d in diffs.Take(20))
                        sb.AppendLine($"    {d}");
                }
            }

            if (changed > 0)
                AssetDatabase.SaveAssets();
            sb.Append($"# 改了 {changed} / {children.Count} 顆。驗證：up mat <child.mat>（`*` = 本檔 override）");
            return sb.ToString();
        }

        private static void Restore(Material child, Material parent, Snap s)
        {
            var shader = child.shader;
            for (var i = 0; i < shader.GetPropertyCount(); i++)
            {
                var n = shader.GetPropertyName(i);
                if (!s.Props.TryGetValue(n, out var want))
                    continue;
                if (parent != null && want.Equals(PropVal.Read(parent, n, want.Type)))
                {
                    if (child.IsPropertyOverriden(n))
                        child.RevertPropertyOverride(n);
                    if (want.Equals(PropVal.Read(child, n, want.Type)))
                        continue;
                }

                want.Write(child, n);
            }

            // keyword：每一個 local keyword 都顯式設成快照的狀態
            var on = new HashSet<string>(s.Keywords);
            foreach (var kw in shader.keywordSpace.keywords)
            {
                var want = on.Contains(kw.name);
                if (child.IsKeywordEnabled(kw) != want)
                    child.SetKeyword(kw, want);
            }

            // shader 不認得的 keyword（m_InvalidKeywords）不處理 —— 對 render 沒作用
            if (child.renderQueue != s.RenderQueue)
                child.renderQueue = s.RenderQueue;
            if (child.enableInstancing != s.Instancing)
                child.enableInstancing = s.Instancing;
            if (child.doubleSidedGI != s.DoubleSidedGI)
                child.doubleSidedGI = s.DoubleSidedGI;
            if (child.globalIlluminationFlags != s.GIFlags)
                child.globalIlluminationFlags = s.GIFlags;

            var disabled = new HashSet<string>(s.DisabledPasses);
            foreach (var pass in disabled.Union(DisabledPasses(child)).ToList())
            {
                var want = !disabled.Contains(pass);
                if (child.GetShaderPassEnabled(pass) != want)
                    child.SetShaderPassEnabled(pass, want);
            }

            foreach (var kv in s.Locks)
                if (child.IsPropertyLocked(kv.Key) != kv.Value)
                    child.SetPropertyLock(kv.Key, kv.Value);
        }

        private static int CountOverrides(Material m)
        {
            var c = 0;
            for (var i = 0; i < m.shader.GetPropertyCount(); i++)
                if (m.IsPropertyOverriden(m.shader.GetPropertyName(i)))
                    c++;
            return c;
        }

        private static bool IsAncestorOrSelf(Material maybeAncestor, Material m)
        {
            for (var cur = m; cur != null; cur = cur.parent)
                if (cur == maybeAncestor)
                    return true;
            return false;
        }

        private static string ShaderName(Material m) => m.shader ? m.shader.name : "（無）";

        private static Material LoadMat(string path, string role)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                return mat;
            if (File.Exists(path))
                throw new Abort($"{role} {path} 在磁碟上但 Unity 載不到 Material（還沒 import？Editor 按 Cmd+R）");
            throw new Abort($"{role} 找不到：{path}{Candidates(path)}");
        }

        /// <summary>同檔名（不分大小寫）或檔名片段的 .mat 候選，最多 6 個。</summary>
        private static string Candidates(string path)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(stem))
                return "";
            var hits = AssetDatabase.FindAssets($"{stem} t:Material")
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".mat")).Take(6).ToList();
            if (hits.Count == 0)
            {
                var token = stem.Split('_', ' ').FirstOrDefault(t => t.Length >= 3) ?? stem;
                hits = AssetDatabase.FindAssets($"{token} t:Material")
                    .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".mat")).Take(6).ToList();
            }

            return hits.Count == 0 ? "（也找不到同名 .mat）" : "\n  你可能想要：\n    " + string.Join("\n    ", hits);
        }

        private static List<string> DisabledPasses(Material m)
        {
            var list = new List<string>();
            var prop = new SerializedObject(m).FindProperty("disabledShaderPasses");
            if (prop == null || !prop.isArray || prop.propertyType == SerializedPropertyType.String)
                return list;
            for (var i = 0; i < prop.arraySize; i++)
                list.Add(prop.GetArrayElementAtIndex(i).stringValue);
            return list;
        }

        private readonly struct PropVal
        {
            public readonly ShaderPropertyType Type;
            private readonly Vector4 _v;
            private readonly Texture _tex;
            private readonly Vector2 _scale;
            private readonly Vector2 _offset;

            private PropVal(ShaderPropertyType type, Vector4 v, Texture tex, Vector2 scale, Vector2 offset)
            {
                Type = type;
                _v = v;
                _tex = tex;
                _scale = scale;
                _offset = offset;
            }

            public static PropVal Read(Material m, string n, ShaderPropertyType t)
            {
                switch (t)
                {
                    case ShaderPropertyType.Color: return new PropVal(t, m.GetColor(n), null, default, default);
                    case ShaderPropertyType.Vector: return new PropVal(t, m.GetVector(n), null, default, default);
                    case ShaderPropertyType.Int: return new PropVal(t, new Vector4(m.GetInteger(n), 0), null, default, default);
                    case ShaderPropertyType.Texture:
                        return new PropVal(t, default, m.GetTexture(n), m.GetTextureScale(n), m.GetTextureOffset(n));
                    default: return new PropVal(t, new Vector4(m.GetFloat(n), 0), null, default, default);
                }
            }

            public void Write(Material m, string n)
            {
                switch (Type)
                {
                    case ShaderPropertyType.Color: m.SetColor(n, _v); break;
                    case ShaderPropertyType.Vector: m.SetVector(n, _v); break;
                    case ShaderPropertyType.Int: m.SetInteger(n, (int)_v.x); break;
                    case ShaderPropertyType.Texture:
                        m.SetTexture(n, _tex);
                        m.SetTextureScale(n, _scale);
                        m.SetTextureOffset(n, _offset);
                        break;
                    default: m.SetFloat(n, _v.x); break;
                }
            }

            public bool Equals(PropVal o) =>
                Type == o.Type && _v == o._v && _tex == o._tex && _scale == o._scale && _offset == o._offset;

            public override string ToString()
            {
                switch (Type)
                {
                    case ShaderPropertyType.Texture:
                        return $"{(_tex ? AssetDatabase.GetAssetPath(_tex) : "none")} s{_scale} o{_offset}";
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                    case ShaderPropertyType.Int:
                        return _v.x.ToString("R");
                    default: return _v.ToString("F4");
                }
            }
        }

        /// <summary>一顆材質「合併後」的完整狀態（走 Material API，所以已經含 parent 鏈）。</summary>
        private sealed class Snap
        {
            public readonly Dictionary<string, PropVal> Props = new Dictionary<string, PropVal>();
            public readonly Dictionary<string, bool> Locks = new Dictionary<string, bool>();
            public string[] Keywords;
            public int RenderQueue;
            public bool Instancing;
            public bool DoubleSidedGI;
            public MaterialGlobalIlluminationFlags GIFlags;
            public List<string> DisabledPasses;

            public static Snap Take(Material m)
            {
                var s = new Snap();
                var shader = m.shader;
                for (var i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var n = shader.GetPropertyName(i);
                    s.Props[n] = PropVal.Read(m, n, shader.GetPropertyType(i));
                    s.Locks[n] = m.IsPropertyLocked(n);
                }

                s.Keywords = m.shaderKeywords.OrderBy(k => k).ToArray();
                s.RenderQueue = m.renderQueue;
                s.Instancing = m.enableInstancing;
                s.DoubleSidedGI = m.doubleSidedGI;
                s.GIFlags = m.globalIlluminationFlags;
                s.DisabledPasses = MatEdit.DisabledPasses(m).OrderBy(p => p).ToList();
                return s;
            }

            public List<string> Diff(Snap o)
            {
                var d = new List<string>();
                foreach (var kv in Props)
                    if (o.Props.TryGetValue(kv.Key, out var ov) && !kv.Value.Equals(ov))
                        d.Add($"{kv.Key}: {kv.Value} → {ov}");
                foreach (var kv in Locks)
                    if (o.Locks.TryGetValue(kv.Key, out var ol) && ol != kv.Value)
                        d.Add($"lock {kv.Key}: {kv.Value} → {ol}");
                if (!Keywords.SequenceEqual(o.Keywords))
                    d.Add($"keywords: [{string.Join(",", Keywords.Except(o.Keywords))}] 不見、" +
                          $"[{string.Join(",", o.Keywords.Except(Keywords))}] 多出來");
                if (RenderQueue != o.RenderQueue) d.Add($"renderQueue: {RenderQueue} → {o.RenderQueue}");
                if (Instancing != o.Instancing) d.Add($"enableInstancing: {Instancing} → {o.Instancing}");
                if (DoubleSidedGI != o.DoubleSidedGI) d.Add($"doubleSidedGI: {DoubleSidedGI} → {o.DoubleSidedGI}");
                if (GIFlags != o.GIFlags) d.Add($"GI flags: {GIFlags} → {o.GIFlags}");
                if (!DisabledPasses.SequenceEqual(o.DisabledPasses))
                    d.Add($"disabledShaderPasses: [{string.Join(",", DisabledPasses)}] → [{string.Join(",", o.DisabledPasses)}]");
                return d;
            }
        }
    }
}
#endif
