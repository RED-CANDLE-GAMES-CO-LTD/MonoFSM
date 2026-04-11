using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor.ReferenceSystem
{
    /// <summary>
    /// 泛化的 Reference Finder — 可查找任意 Object 被引用的位置
    /// </summary>
    public class ComponentReferenceWindow : EditorWindow
    {
        private enum ScanMode
        {
            PrefabStage,
            Scene
        }

        private ScanMode _scanMode = ScanMode.PrefabStage;
        private Object _selectedTarget;
        private bool _locked;
        private Vector2 _scrollPos;

        private List<ComponentReferenceInfo> _localReferences = new();
        private List<ComponentReferenceInfo> _crossEntityReferences = new();

        private bool _localFoldout = true;
        private bool _crossEntityFoldout = true;

        private GameObject _sceneRoot;

        [MenuItem("Tools/MonoFSM/Component Reference Finder")]
        public static void ShowWindow()
        {
            GetWindow<ComponentReferenceWindow>("Reference Finder");
        }

        /// <summary>
        /// 開啟視窗並直接查找指定的 Object
        /// </summary>
        public static void ShowWindowWithTarget(Object target)
        {
            var window = GetWindow<ComponentReferenceWindow>("Reference Finder");

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                ComponentReferenceScanner.ScanFromRoot(stage.prefabContentsRoot);
            }
            else if (target is Component comp)
            {
                var root = comp.transform.root.gameObject;
                ComponentReferenceScanner.ScanFromRoot(root);
                window._sceneRoot = root;
                window._scanMode = ScanMode.Scene;
            }

            window._selectedTarget = target;
            window._locked = true;
            window.UpdateSearchResults();
            window.Repaint();
        }

        private void OnEnable()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            Selection.selectionChanged += OnSelectionChanged;

            TryScanCurrentPrefabStage();
        }

        private void OnDisable()
        {
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            Selection.selectionChanged -= OnSelectionChanged;
            ComponentReferenceScanner.ClearCache();
        }

        private void OnPrefabStageOpened(PrefabStage stage)
        {
            if (_scanMode == ScanMode.PrefabStage)
            {
                ComponentReferenceScanner.ScanFromRoot(stage.prefabContentsRoot);
                ClearSelection();
                Repaint();
            }
        }

        private void OnPrefabStageClosing(PrefabStage stage)
        {
            if (_scanMode == ScanMode.PrefabStage)
            {
                ComponentReferenceScanner.ClearCache();
                ClearSelection();
                Repaint();
            }
        }

        private void OnSelectionChanged()
        {
            if (_locked) return;

            if (Selection.activeObject != null && Selection.activeObject != _selectedTarget)
            {
                _selectedTarget = Selection.activeObject;
                UpdateSearchResults();
                Repaint();
            }
        }

        private void TryScanCurrentPrefabStage()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                ComponentReferenceScanner.ScanFromRoot(stage.prefabContentsRoot);
        }

        private void ClearSelection()
        {
            _selectedTarget = null;
            _localReferences.Clear();
            _crossEntityReferences.Clear();
        }

        private void UpdateSearchResults()
        {
            _localReferences.Clear();
            _crossEntityReferences.Clear();

            if (_selectedTarget == null) return;

            var allRefs = ComponentReferenceScanner.GetReferences(_selectedTarget);

            foreach (var refInfo in allRefs)
            {
                if (refInfo.ReferencingComponent == _selectedTarget) continue;

                if (refInfo.Scope == ReferenceScope.Local)
                    _localReferences.Add(refInfo);
                else
                    _crossEntityReferences.Add(refInfo);
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawModeAndRoot();
            DrawTargetSelector();
            DrawResults();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("Reference Finder", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(_scanMode == ScanMode.PrefabStage);
            if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (_sceneRoot != null)
                {
                    ComponentReferenceScanner.ScanFromRoot(_sceneRoot);
                    UpdateSearchResults();
                }
            }
            EditorGUI.EndDisabledGroup();

            var lockContent = _locked
                ? EditorGUIUtility.IconContent("LockIcon-On")
                : EditorGUIUtility.IconContent("LockIcon");
            if (GUILayout.Button(lockContent, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                _locked = !_locked;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeAndRoot()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            _scanMode = (ScanMode)EditorGUILayout.EnumPopup("Mode", _scanMode, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                if (_scanMode == ScanMode.PrefabStage)
                    TryScanCurrentPrefabStage();
                else
                    ComponentReferenceScanner.ClearCache();
                ClearSelection();
            }

            if (_scanMode == ScanMode.PrefabStage)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                var rootName = stage?.prefabContentsRoot?.name ?? "(No Prefab Open)";
                EditorGUILayout.LabelField($"Root: {rootName}");
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                _sceneRoot = EditorGUILayout.ObjectField("Root", _sceneRoot, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck() && _sceneRoot != null)
                {
                    ComponentReferenceScanner.ScanFromRoot(_sceneRoot);
                    UpdateSearchResults();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (_scanMode == ScanMode.PrefabStage)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage == null)
                    EditorGUILayout.HelpBox("Please open a Prefab to use PrefabStage mode.", MessageType.Info);
            }
        }

        private void DrawTargetSelector()
        {
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            _selectedTarget = EditorGUILayout.ObjectField(
                "Select Target", _selectedTarget, typeof(Object), true);

            if (EditorGUI.EndChangeCheck())
                UpdateSearchResults();

            if (_selectedTarget == null)
                EditorGUILayout.HelpBox("Select any Object to find its references.", MessageType.Info);
        }

        private void DrawResults()
        {
            if (_selectedTarget == null) return;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            _localFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                _localFoldout, $"Local References ({_localReferences.Count})");
            if (_localFoldout)
            {
                EditorGUI.indentLevel++;
                if (_localReferences.Count == 0)
                    EditorGUILayout.LabelField("No local references found.", EditorStyles.miniLabel);
                else
                    foreach (var refInfo in _localReferences)
                        DrawReferenceItem(refInfo);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            _crossEntityFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                _crossEntityFoldout, $"Cross-Entity References ({_crossEntityReferences.Count})");
            if (_crossEntityFoldout)
            {
                EditorGUI.indentLevel++;
                if (_crossEntityReferences.Count == 0)
                    EditorGUILayout.LabelField("No cross-entity references found.", EditorStyles.miniLabel);
                else
                    foreach (var refInfo in _crossEntityReferences)
                        DrawReferenceItem(refInfo);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndScrollView();
        }

        private void DrawReferenceItem(ComponentReferenceInfo refInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(refInfo.ComponentDisplayName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{refInfo.TypeDisplayName}]", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Field: {refInfo.FieldPath}", EditorStyles.miniLabel);

            if (refInfo.Scope == ReferenceScope.CrossEntity && refInfo.OwnerEntity != null)
                EditorGUILayout.LabelField($"Entity: {refInfo.OwnerEntity.name}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
                EditorGUIUtility.PingObject(refInfo.ReferencingComponent);

            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = refInfo.ReferencingComponent;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
