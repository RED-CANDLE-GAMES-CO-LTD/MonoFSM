using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Animation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MonoFSM.Editor.AnimatorParamReferenceSystem
{
    /// <summary>
    /// Animator Parameter Reference Finder - 反查哪些 Action 設定了哪些 Animator 參數
    /// </summary>
    public class AnimatorParamReferenceWindow : EditorWindow
    {
        private enum ScanMode
        {
            PrefabStage,
            Scene
        }

        // UI 狀態
        private ScanMode _scanMode = ScanMode.PrefabStage;
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private GameObject _sceneRoot;
        private Animator _animatorFilter;

        // Foldout 狀態 (paramName -> foldout)
        private Dictionary<string, bool> _foldoutStates = new();

        [MenuItem("Tools/MonoFSM/Animator Param Reference Finder")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorParamReferenceWindow>("Animator Param Finder");
        }

        // --- Context Menus ---

        [MenuItem("CONTEXT/AbstractAnimatorSetValueAction/Find All Param Setters")]
        private static void FindFromAbstractAction(MenuCommand command)
        {
            ShowWindowAndScan(command.context as Component);
        }

        [MenuItem("CONTEXT/AnimatorParameterSetValueAction/Find All Param Setters")]
        private static void FindFromParamAction(MenuCommand command)
        {
            ShowWindowAndScan(command.context as Component);
        }

        [MenuItem("CONTEXT/AnimatorPlayAction/Find All Param Setters")]
        private static void FindFromPlayAction(MenuCommand command)
        {
            ShowWindowAndScan(command.context as Component);
        }

        [MenuItem("CONTEXT/Animator/Find All Param Setters")]
        private static void FindFromAnimator(MenuCommand command)
        {
            ShowWindowAndScan(command.context as Component);
        }

        /// <summary>
        /// 從外部開啟並自動掃描
        /// </summary>
        public static void ShowWindowAndScan(Component context)
        {
            var window = GetWindow<AnimatorParamReferenceWindow>("Animator Param Finder");

            // 根據 context 類型自動設定 Animator Filter
            window._animatorFilter = ResolveAnimator(context);

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                window._scanMode = ScanMode.PrefabStage;
                AnimatorParamScanner.ScanFromRoot(stage.prefabContentsRoot);
            }
            else if (context != null)
            {
                var root = context.transform.root.gameObject;
                AnimatorParamScanner.ScanFromRoot(root);
                window._sceneRoot = root;
                window._scanMode = ScanMode.Scene;
            }

            window.Repaint();
        }

        private static Animator ResolveAnimator(Component context)
        {
            if (context == null) return null;

            // 直接從 Animator 右鍵
            if (context is Animator animator)
                return animator;

            // 從 AbstractAnimatorSetValueAction 右鍵
            if (context is AbstractAnimatorSetValueAction setValueAction)
            {
                var prop = typeof(AbstractAnimatorSetValueAction)
                    .GetProperty("Animator", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                return prop?.GetValue(setValueAction) as Animator;
            }

            // 從 AnimatorParameterSetValueAction 右鍵
            if (context is AnimatorParameterSetValueAction paramAction)
            {
                var prop = typeof(AnimatorParameterSetValueAction)
                    .GetProperty("animator", BindingFlags.Instance | BindingFlags.NonPublic);
                return prop?.GetValue(paramAction) as Animator;
            }

            // 從 AnimatorPlayAction 右鍵
            if (context is AnimatorPlayAction playAction)
            {
                return playAction._animator;
            }

            return null;
        }

        private void OnEnable()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            TryScanCurrentPrefabStage();
        }

        private void OnDisable()
        {
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            AnimatorParamScanner.ClearCache();
        }

        private void OnPrefabStageOpened(PrefabStage stage)
        {
            if (_scanMode == ScanMode.PrefabStage)
            {
                AnimatorParamScanner.ScanFromRoot(stage.prefabContentsRoot);
                Repaint();
            }
        }

        private void OnPrefabStageClosing(PrefabStage stage)
        {
            if (_scanMode == ScanMode.PrefabStage)
            {
                AnimatorParamScanner.ClearCache();
                Repaint();
            }
        }

        private void TryScanCurrentPrefabStage()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                AnimatorParamScanner.ScanFromRoot(stage.prefabContentsRoot);
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawModeAndRoot();
            DrawSearchFilter();
            DrawResults();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Animator Param Reference Finder", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (_scanMode == ScanMode.PrefabStage)
                    TryScanCurrentPrefabStage();
                else if (_sceneRoot != null)
                    AnimatorParamScanner.ScanFromRoot(_sceneRoot);
                Repaint();
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
                    AnimatorParamScanner.ClearCache();
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
                    AnimatorParamScanner.ScanFromRoot(_sceneRoot);
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

        private void DrawSearchFilter()
        {
            EditorGUILayout.Space(3);
            _animatorFilter = EditorGUILayout.ObjectField("Animator Filter", _animatorFilter, typeof(Animator), true) as Animator;
            _searchFilter = EditorGUILayout.TextField("Param Filter", _searchFilter);
            EditorGUILayout.Space(3);
        }

        private void DrawResults()
        {
            var allCache = AnimatorParamScanner.GetAllCache();
            if (allCache.Count == 0)
            {
                EditorGUILayout.HelpBox("No Animator parameter setters found. Click Scan to refresh.", MessageType.Info);
                return;
            }

            // 依參數名稱排序
            var sortedParams = allCache.Keys.OrderBy(k => k).ToList();

            // 套用搜尋過濾
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                var filter = _searchFilter.ToLowerInvariant();
                sortedParams = sortedParams.Where(p => p.ToLowerInvariant().Contains(filter)).ToList();
            }

            EditorGUILayout.LabelField($"Parameters: {sortedParams.Count}", EditorStyles.miniLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var paramName in sortedParams)
            {
                var infos = allCache[paramName];

                // 套用 Animator 過濾
                if (_animatorFilter != null)
                    infos = infos.Where(i => i.TargetAnimator == _animatorFilter).ToList();

                if (infos.Count == 0) continue;

                if (!_foldoutStates.TryGetValue(paramName, out var foldout))
                    foldout = false;

                _foldoutStates[paramName] = EditorGUILayout.BeginFoldoutHeaderGroup(
                    foldout, $"{paramName}  ({infos.Count} setters)");

                if (_foldoutStates[paramName])
                {
                    EditorGUI.indentLevel++;
                    foreach (var info in infos)
                    {
                        DrawParamItem(info);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawParamItem(AnimatorParamInfo info)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 第一行：Action 類型 + State 名稱
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(info.ActionTypeName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{info.StateName}]", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 第二行：Description
            if (!string.IsNullOrEmpty(info.ActionDescription))
            {
                EditorGUILayout.LabelField(info.ActionDescription, EditorStyles.miniLabel);
            }

            // 第三行：Animator 目標
            if (info.TargetAnimator != null)
            {
                EditorGUILayout.LabelField($"Animator: {info.TargetAnimator.gameObject.name}", EditorStyles.miniLabel);
            }

            // 按鈕列
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(info.ActionComponent);
            }

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeObject = info.ActionComponent;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
