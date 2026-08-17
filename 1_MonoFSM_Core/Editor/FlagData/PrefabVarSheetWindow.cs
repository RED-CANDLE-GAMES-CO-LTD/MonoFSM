using System.Collections.Generic;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.FlagData
{
    /// <summary>
    ///     Prefab-Var Sheet：把一個 prefab 家族（base + 所有 variant）的 VarFloat 預設值攤成表格。
    ///     rows = base + variants，columns = 家族 schema 聯集（VariableTag），
    ///     cell = 該 prefab 上該 tag 的 VarFloat 的 FlagField 預設值（_localField.ProductionValue）。
    ///     沒有那顆 var 的 cell 顯示 "-"；variant 上有 override 的高亮，繼承值灰字。
    ///     寫回走 SerializedObject + ApplyModifiedProperties（Unity 自己記 m_Modifications），不碰 YAML。
    ///     入口：Tools/MonoFSM/Prefab Var Sheet
    /// </summary>
    public class PrefabVarSheetWindow : OdinEditorWindow
    {
        private const string LogTag = "[PrefabVarSheet]";
        private const int MaxVariantChainDepth = 16;

        [MenuItem("Tools/MonoFSM/Prefab Var Sheet")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabVarSheetWindow>();
            window.titleContent = new GUIContent("Prefab Var Sheet");
            window.minSize = new Vector2(600, 300);
            window.Show();

            if (Selection.activeObject is GameObject go && AssetDatabase.Contains(go))
            {
                window._basePrefab = go;
                window.Rebuild();
            }
        }

        [Title("Base Prefab")]
        [AssetsOnly]
        [OnValueChanged(nameof(Rebuild))]
        [SerializeField]
        private GameObject _basePrefab;

        [Button("重新掃描", ButtonSizes.Medium)]
        private void Rebuild()
        {
            _rows.Clear();
            _columns.Clear();
            if (_basePrefab == null)
                return;

            var familyPaths = CollectFamilyPaths(_basePrefab);
            foreach (var path in familyPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;
                var depth = GetVariantDepth(prefab, _basePrefab);
                if (depth < 0)
                    continue;
                _rows.Add(BuildRow(prefab, depth));
            }

            _rows.Sort((a, b) =>
            {
                var d = a.Depth.CompareTo(b.Depth);
                return d != 0 ? d : string.CompareOrdinal(a.Prefab.name, b.Prefab.name);
            });

            //columns：base 先、其他 variant 新增的欄位接在後面
            foreach (var row in _rows)
            foreach (var tag in row.OrderedTags)
                if (!_columns.Contains(tag))
                    _columns.Add(tag);

            Debug.Log($"{LogTag} {_basePrefab.name}：{_rows.Count} 個 prefab、{_columns.Count} 個 tag 欄位");
        }

        #region Data

        private class Row
        {
            public GameObject Prefab;
            public int Depth;
            public readonly List<VariableTag> OrderedTags = new();
            public readonly Dictionary<VariableTag, VarFloat> Vars = new();
            public readonly Dictionary<VariableTag, SerializedObject> SerializedObjects = new();
        }

        private readonly List<Row> _rows = new();
        private readonly List<VariableTag> _columns = new();
        private Vector2 _scroll;

        private static Row BuildRow(GameObject prefab, int depth)
        {
            var row = new Row { Prefab = prefab, Depth = depth };
            foreach (var varFloat in prefab.GetComponentsInChildren<VarFloat>(true))
            {
                var tag = varFloat._varTag;
                if (tag == null || row.Vars.ContainsKey(tag))
                    continue;
                row.OrderedTags.Add(tag);
                row.Vars.Add(tag, varFloat);
                row.SerializedObjects.Add(tag, new SerializedObject(varFloat));
            }

            return row;
        }

        /// <summary>
        ///     用非遞迴依賴做反向索引 BFS，找出 base prefab 的整個 variant 家族（含 base 自己）。
        ///     variant 一定會依賴它的直接 parent prefab，所以 BFS 能一層層展開。
        /// </summary>
        private static List<string> CollectFamilyPaths(GameObject basePrefab)
        {
            var basePath = AssetDatabase.GetAssetPath(basePrefab);
            var result = new List<string> { basePath };
            if (string.IsNullOrEmpty(basePath))
                return result;

            //dep path -> 依賴它的 prefab paths
            var dependents = new Dictionary<string, List<string>>();
            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || path == basePath)
                    continue;
                var deps = AssetDatabase.GetDependencies(path, false);
                for (var d = 0; d < deps.Length; d++)
                {
                    var dep = deps[d];
                    if (dep == path || !dep.EndsWith(".prefab"))
                        continue;
                    if (!dependents.TryGetValue(dep, out var list))
                    {
                        list = new List<string>();
                        dependents.Add(dep, list);
                    }

                    list.Add(path);
                }
            }

            var visited = new HashSet<string> { basePath };
            var queue = new Queue<string>();
            queue.Enqueue(basePath);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!dependents.TryGetValue(current, out var children))
                    continue;
                foreach (var child in children)
                {
                    if (!visited.Add(child))
                        continue;
                    //只是「引用到」不算家族，真正的 variant 由 GetVariantDepth 再驗一次
                    result.Add(child);
                    queue.Enqueue(child);
                }
            }

            return result;
        }

        //回傳到 base 的 variant 階層深度；base 自己是 0，不是它的 variant 回 -1
        private static int GetVariantDepth(GameObject prefab, GameObject basePrefab)
        {
            if (prefab == basePrefab)
                return 0;
            var current = prefab;
            for (var i = 1; i <= MaxVariantChainDepth; i++)
            {
                current = PrefabUtility.GetCorrespondingObjectFromSource(current);
                if (current == null)
                    return -1;
                if (current == basePrefab)
                    return i;
            }

            return -1;
        }

        #endregion

        #region GUI

        private const float RowLabelWidth = 260f;
        private const float CellWidth = 90f;
        private const float RowHeight = 20f;

        private static GUIStyle _inheritedStyle;
        private static GUIStyle _overrideStyle;

        private static void EnsureStyles()
        {
            if (_inheritedStyle == null)
            {
                _inheritedStyle = new GUIStyle(EditorStyles.numberField);
                _inheritedStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
            }

            if (_overrideStyle == null)
            {
                _overrideStyle = new GUIStyle(EditorStyles.numberField) { fontStyle = FontStyle.Bold };
                _overrideStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            }
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();

            if (_rows.Count == 0)
            {
                EditorGUILayout.HelpBox("選一顆 base prefab 後按「重新掃描」", MessageType.Info);
                return;
            }

            EnsureStyles();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            //header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel, GUILayout.Width(RowLabelWidth));
            foreach (var tag in _columns)
                EditorGUILayout.LabelField(
                    new GUIContent(tag.name, tag.name),
                    EditorStyles.miniBoldLabel,
                    GUILayout.Width(CellWidth)
                );
            EditorGUILayout.EndHorizontal();

            foreach (var row in _rows)
                DrawRow(row);

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(Row row)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

            var indent = row.Depth * 12f;
            GUILayout.Space(indent);
            if (GUILayout.Button(
                    new GUIContent(row.Prefab.name, AssetDatabase.GetAssetPath(row.Prefab)),
                    EditorStyles.label,
                    GUILayout.Width(RowLabelWidth - indent)
                ))
            {
                Selection.activeObject = row.Prefab;
                EditorGUIUtility.PingObject(row.Prefab);
            }

            foreach (var tag in _columns)
                DrawCell(row, tag);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCell(Row row, VariableTag tag)
        {
            if (!row.Vars.TryGetValue(tag, out var varFloat) || varFloat == null)
            {
                EditorGUILayout.LabelField("-", GUILayout.Width(CellWidth));
                return;
            }

            var so = row.SerializedObjects[tag];
            so.UpdateIfRequiredOrScript();
            var fieldProp = so.FindProperty("_localField");
            var prop = fieldProp?.FindPropertyRelative("ProductionValue");
            if (prop == null)
            {
                EditorGUILayout.LabelField("?", GUILayout.Width(CellWidth));
                return;
            }

            //base row 的值就是自己的值；variant 上沒 override 就是繼承來的
            var style =
                row.Depth == 0 ? EditorStyles.numberField
                : prop.prefabOverride ? _overrideStyle
                : _inheritedStyle;

            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.DelayedFloatField(
                prop.floatValue,
                style,
                GUILayout.Width(CellWidth)
            );
            if (!EditorGUI.EndChangeCheck())
                return;

            prop.floatValue = newValue;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(varFloat);
            AssetDatabase.SaveAssetIfDirty(row.Prefab);
            Debug.Log(
                $"{LogTag} {row.Prefab.name}.{tag.name} = {newValue}",
                row.Prefab
            );
        }

        #endregion
    }
}
