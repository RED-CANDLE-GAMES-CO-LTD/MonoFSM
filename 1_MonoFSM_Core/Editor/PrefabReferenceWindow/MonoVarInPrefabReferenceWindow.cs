using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using MonoFSM.Variable;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Experimental.SceneManagement;

public class MonoVarInPrefabReferenceWindow : EditorWindow
{
    private class ReferenceInfo
    {
        public Component component;
        public FieldInfo fieldInfo;
        public object value;
    }

    private static Dictionary<IReferenceTarget, List<ReferenceInfo>> _referenceCache = new();
    private static GameObject _cachedRoot;
    private static string _cachedPrefabAssetPath;

    private IReferenceTarget _searchTarget;
    private bool _locked = false; 
    private Vector2 _scrollPos;
    private List<ReferenceInfo> _searchResults = new();

    [MenuItem("Tools/Prefab Variable Reference Finder")]
    public static void ShowWindow()
    {
        GetWindow<MonoVarInPrefabReferenceWindow>("Prefab Variable Reference Finder");
    }

    private void OnEnable()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        Selection.selectionChanged += OnSelectionChanged;
        TryCacheIfInPrefabStage();
    }

    private void OnDisable()
    {
        PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        Selection.selectionChanged -= OnSelectionChanged;
        ClearCache();
    }

    private void OnSelectionChanged()
    {
        if (_locked) return; 
        IReferenceTarget target = null;
        if (Selection.activeGameObject)
            // 取第一個有 IReferenceTarget 的 component
            target = Selection.activeGameObject.GetComponents<Component>().OfType<IReferenceTarget>().FirstOrDefault();
        if (!Equals(_searchTarget, target))
        {
            _searchTarget = target;
            UpdateSearchResults();
            Repaint();
        }
    }

    private void OnPrefabStageOpened(PrefabStage stage)
    {
        CacheReferences(stage);
    }

    private void OnPrefabStageClosing(PrefabStage stage)
    {
        ClearCache();
    }

    private void TryCacheIfInPrefabStage()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null) CacheReferences(stage);
    }

    private void CacheReferences(PrefabStage stage)
    {
        _referenceCache.Clear();
        _cachedRoot = stage.prefabContentsRoot;
        _cachedPrefabAssetPath = stage.assetPath;
        if (_cachedRoot == null) return;
        var components = _cachedRoot.GetComponentsInChildren<Component>(true);
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
                if (typeof(IReferenceTarget).IsAssignableFrom(field.FieldType))
                {
                    var value = field.GetValue(comp) as IReferenceTarget;
                    if (value != null)
                    {
                        if (!_referenceCache.TryGetValue(value, out var list))
                        {
                            list = new List<ReferenceInfo>();
                            _referenceCache[value] = list;
                        }

                        list.Add(new ReferenceInfo { component = comp, fieldInfo = field, value = value });
                    }
                }
        }
    }

    private void ClearCache()
    {
        _referenceCache.Clear();
        _cachedRoot = null;
        _cachedPrefabAssetPath = null;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab Variable Reference Finder", EditorStyles.boldLabel);
        if (_cachedRoot == null)
        {
            EditorGUILayout.HelpBox("請在 PrefabStage 下開啟本視窗。", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        _locked = GUILayout.Toggle(_locked, "鎖定", "Button", GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginChangeCheck();
        _searchTarget = (IReferenceTarget)EditorGUILayout.ObjectField(
            "搜尋 Reference Target",
            _searchTarget as UnityEngine.Object,
            typeof(UnityEngine.Object), // or a more specific type if you know it, e.g. typeof(MyReferenceTargetType)
            true
        ) as IReferenceTarget;
        if (EditorGUI.EndChangeCheck()) UpdateSearchResults();
        if (_searchTarget == null)
        {
            EditorGUILayout.HelpBox("請拖曳或選擇一個 Reference Target 來查詢。", MessageType.Info);
            return;
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        if (_searchResults.Count == 0)
            EditorGUILayout.LabelField("找不到任何 reference。", EditorStyles.miniLabel);
        else
            foreach (var info in _searchResults)
            {
                if ((Component)_searchTarget == info.component) continue;
                EditorGUILayout.ObjectField("Component", info.component, typeof(Component), true);
                EditorGUILayout.LabelField($"Field: {info.fieldInfo.Name}");
                EditorGUILayout.Space();
            }

        EditorGUILayout.EndScrollView();
    }

    private void UpdateSearchResults()
    {
        _searchResults.Clear();
        if (_searchTarget == null) return;
        if (_referenceCache.TryGetValue(_searchTarget, out var list)) _searchResults.AddRange(list);
    }
}