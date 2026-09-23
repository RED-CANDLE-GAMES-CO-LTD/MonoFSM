using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// `prefab do` 的「連帶損失」檢查：批次開始時記下這支 prefab 既有的 prefab-instance
    /// override（m_Modifications）與 added GameObject / Component，存檔 reload 後比對，
    /// 這批沒碰過的欄位若 override 不見了、被蓋回 base 值、或 added 節點消失，就印警告。
    ///
    /// 為什麼要有這層：逐欄位驗證（VerifyTouch）只驗「這批寫的東西」，驗不到「這批沒寫、
    /// 但被存檔順手蓋掉的東西」。2026-09-17 PPlayer 整顆 `auto|` 之後，先前寫進 nested
    /// 實例的 `_targets[5]`、GameData Map、hand.L 底下的 view 節點全部退回，驗證卻印 OK ——
    /// 就是這個缺口。只當警告不擋存檔：存檔已經發生，擋也救不回來，要的是「立刻知道」。
    ///
    /// 故意只抓「override 消失 / 值等於 base / 物件引用變 null / added 節點消失」，
    /// 不抓一般的值變動：OnBeforePrefabSave callback 與 [Auto*] 本來就會合法改值，
    /// 全抓只會變成每次都有的噪音，噪音一多警告就沒人看了。
    /// </summary>
    internal sealed class OverrideGuard
    {
        private sealed class ModEntry
        {
            internal string TargetKey;
            internal string PropertyPath;
            internal string Value;
            internal Object Ref;
            internal bool HadRef;
            internal Object Target; // base 端的物件，算「值是否等於 base」用
            internal Object Instance; // contents 裡對應的 instance 物件；被這批刪掉就跳過
            internal string Label;
            internal string CapturedValue;
            internal bool Skip;
        }

        private readonly List<ModEntry> _mods = new();
        private readonly List<Object> _added = new(); // added GameObject / Component（instance 端）
        private readonly List<string> _addedLabels = new();
        private readonly HashSet<string> _excluded = new();

        /// <summary>
        /// 這幾個 propertyPath 是結構操作（rename / 插入兄弟節點 / 自動命名）本來就會動的，
        /// 不列入檢查。
        /// </summary>
        private static bool Ignored(string path) =>
            path == "m_Name" || path == "m_RootOrder" || path.StartsWith("m_LocalEulerAnglesHint");

        internal static string Key(Object o)
        {
            if (o == null) return null;
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out var guid, out long fid)
                ? guid + ":" + fid
                : null;
        }

        private static IEnumerable<GameObject> OutermostRoots(GameObject root) =>
            root.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject)
                .Where(PrefabUtility.IsOutermostPrefabInstanceRoot);

        /// <summary>LoadPrefabContents 之後、任何 op 之前呼叫。</summary>
        internal static OverrideGuard Take(GameObject root)
        {
            var guard = new OverrideGuard();

            // base 端物件 key → contents 裡的 instance 物件（判「被這批刪掉」與組 label 用）
            var instanceByKey = new Dictionary<string, Object>();
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null || !PrefabUtility.IsPartOfPrefabInstance(comp)) continue;
                AddInstance(instanceByKey, comp);
                if (comp is Transform) AddInstance(instanceByKey, comp.gameObject);
            }

            foreach (var instRoot in OutermostRoots(root))
            {
                var mods = PrefabUtility.GetPropertyModifications(instRoot);
                if (mods != null)
                    foreach (var mod in mods)
                    {
                        if (mod?.target == null || Ignored(mod.propertyPath)) continue;
                        var key = Key(mod.target);
                        if (key == null) continue;
                        instanceByKey.TryGetValue(key, out var inst);
                        guard._mods.Add(new ModEntry
                        {
                            TargetKey = key,
                            PropertyPath = mod.propertyPath,
                            Value = mod.value,
                            Ref = mod.objectReference,
                            HadRef = mod.objectReference != null,
                            Target = mod.target,
                            Instance = inst
                        });
                    }

                foreach (var added in PrefabUtility.GetAddedGameObjects(instRoot))
                    if (added.instanceGameObject != null) guard._added.Add(added.instanceGameObject);
                foreach (var added in PrefabUtility.GetAddedComponents(instRoot))
                    if (added.instanceComponent != null) guard._added.Add(added.instanceComponent);
            }

            return guard;
        }

        private static void AddInstance(Dictionary<string, Object> map, Object instance)
        {
            var key = Key(PrefabUtility.GetCorrespondingObjectFromSource(instance));
            if (key != null) map[key] = instance;
        }

        /// <summary>
        /// 這批明確寫過的欄位不檢查（值本來就該變）。field 為 null = 整顆物件都不檢查
        /// （transform / active 這類不走 SerializedProperty 路徑的寫入）。
        /// </summary>
        internal void Exclude(Object instance, string field)
        {
            if (instance == null) return;
            var key = Key(PrefabUtility.GetCorrespondingObjectFromSource(instance));
            if (key == null) return;
            _excluded.Add(key + "|" + (field == null ? "*" : TopField(field)));
        }

        private static string TopField(string path)
        {
            var dot = path.IndexOf('.');
            return dot < 0 ? path : path.Substring(0, dot);
        }

        /// <summary>SaveAsPrefabAsset 之後、Unload 之前呼叫（路徑、引用 identity 才穩定）。</summary>
        internal void Capture(Transform root)
        {
            foreach (var e in _mods)
            {
                // instance 被這批刪掉 / 引用目標被刪掉 → 消失是預期的
                if (e.Instance == null || (e.HadRef && e.Ref == null) ||
                    _excluded.Contains(e.TargetKey + "|*") ||
                    _excluded.Contains(e.TargetKey + "|" + TopField(e.PropertyPath)))
                {
                    e.Skip = true;
                    continue;
                }

                e.CapturedValue = e.HadRef ? PrefabEdit.ReferenceKey(e.Ref, root) : e.Value;
                e.Label = LabelOf(root, e.Instance) + "." + e.PropertyPath;
            }

            _addedLabels.Clear();
            foreach (var o in _added)
            {
                if (o == null) continue; // 這批刪掉的
                _addedLabels.Add(LabelOf(root, o));
            }
        }

        private static string LabelOf(Transform root, Object o)
        {
            var t = o switch
            {
                GameObject go => go.transform,
                Component c => c.transform,
                _ => null
            };
            var path = EditResolve.Describe(t == null ? null : EditResolve.PathOf(root, t));
            return o is Component comp && !(comp is Transform) ? path + "." + comp.GetType().Name : path;
        }

        /// <summary>回傳警告行（不含前綴）；空 = 沒有連帶損失。</summary>
        internal List<string> Compare(Transform reloaded)
        {
            var result = new List<string>();
            var post = new Dictionary<string, PropertyModification>();
            var postAdded = new HashSet<string>();
            foreach (var instRoot in OutermostRoots(reloaded.gameObject))
            {
                var mods = PrefabUtility.GetPropertyModifications(instRoot);
                if (mods != null)
                    foreach (var mod in mods)
                    {
                        if (mod?.target == null) continue;
                        var key = Key(mod.target);
                        if (key != null) post[key + "|" + mod.propertyPath] = mod;
                    }

                foreach (var added in PrefabUtility.GetAddedGameObjects(instRoot))
                    if (added.instanceGameObject != null)
                        postAdded.Add(LabelOf(reloaded, added.instanceGameObject));
                foreach (var added in PrefabUtility.GetAddedComponents(instRoot))
                    if (added.instanceComponent != null)
                        postAdded.Add(LabelOf(reloaded, added.instanceComponent));
            }

            foreach (var e in _mods)
            {
                if (e.Skip || e.CapturedValue == null) continue;
                if (!post.TryGetValue(e.TargetKey + "|" + e.PropertyPath, out var now))
                {
                    result.Add($"{e.Label}：override 不見了（原本 {Short(e.CapturedValue)}，現在回到 base）");
                    continue;
                }

                if (e.HadRef)
                {
                    if (now.objectReference == null)
                        result.Add($"{e.Label}：引用從 {Short(e.CapturedValue)} 變成 null");
                    continue;
                }

                if (now.value == e.Value) continue;
                var baseProp = e.Target == null
                    ? null
                    : new SerializedObject(e.Target).FindProperty(e.PropertyPath);
                if (baseProp != null && BaseValueString(baseProp) == now.value)
                    result.Add($"{e.Label}：{Short(e.Value)} 被蓋回 base 值 {Short(now.value)}");
            }

            foreach (var label in _addedLabels)
                if (!postAdded.Contains(label))
                    result.Add($"{label}：這批之前就加在 nested 實例上的節點 / component 存檔後不見了");

            return result;
        }

        /// <summary>
        /// 把 base 端 SerializedProperty 轉成 PropertyModification.value 的字串格式，只做常見的
        /// 純量；轉不出來回 null（= 不判定「蓋回 base」，寧可漏報不要誤報）。
        /// </summary>
        private static string BaseValueString(SerializedProperty p) => p.propertyType switch
        {
            SerializedPropertyType.Integer => p.longValue.ToString(),
            SerializedPropertyType.Boolean => p.boolValue ? "1" : "0",
            SerializedPropertyType.Enum => p.intValue.ToString(),
            SerializedPropertyType.LayerMask => p.intValue.ToString(),
            SerializedPropertyType.ArraySize => p.intValue.ToString(),
            SerializedPropertyType.Float => p.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SerializedPropertyType.String => p.stringValue,
            _ => null
        };

        private static string Short(string s) =>
            s == null ? "null" : s.Length <= 80 ? s : s.Substring(0, 77) + "...";
    }
}
