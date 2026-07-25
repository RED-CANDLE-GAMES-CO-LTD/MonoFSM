using System;
using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using _1_MonoFSM_Core.Runtime.LifeCycle.Update;
using MonoFSM.FSM;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.FSMMonitor
{
    /// <summary>
    /// FSM Monitor - 用於快速預覽 FSM 的 Variables 和 States 結構
    /// 支援自動追蹤 Hierarchy 選擇，並提供狀態跳轉功能
    /// </summary>
    [Searchable]
    public class FsmMonitorWindow : OdinEditorWindow
    {
        [MenuItem("Tools/MonoFSM/FSM Monitor")]
        private static void OpenWindow()
        {
            GetWindow<FsmMonitorWindow>("FSM Monitor").Show();
        }

        // === 當前監控的目標 ===
        [Title("目標")]
        [ShowInInspector, ReadOnly]
        [LabelText("MonoEntity")]
        private MonoEntity _currentEntity;

        [ShowInInspector, ReadOnly]
        [LabelText("StateMachineLogic")]
        private StateMachineLogic _currentFsm;

        // [ShowInInspector, ReadOnly]
        [LabelText("Path")]
        private string _entityPath;

        // [HorizontalGroup("Buttons")]
        // [Button("刷新", ButtonSizes.Medium)]
        private void RefreshAll()
        {
            if (_currentEntity != null)
            {
                RefreshEntityHierarchy();
                RefreshStates();
                RefreshVariables();
            }
        }

        // [HorizontalGroup("Buttons")]
        // [Button("清除", ButtonSizes.Medium)]
        // private void ClearSelection()
        // {
        //     _currentEntity = null;
        //     _currentFsm = null;
        //     _entityPath = null;
        //     _owningMonoObj = null;
        //     _hierarchyNodes.Clear();
        //     _states.Clear();
        //     _variables.Clear();
        // }

        [HorizontalGroup("Buttons")]
        [Button("★ 加入最愛", ButtonSizes.Medium)]
        [EnableIf(nameof(HasCurrentEntity))]
        private void AddToFavorite()
        {
            if (_currentEntity == null) return;

            // 檢查是否已存在（用快取的引用比對）
            foreach (var entry in _favoriteEntries)
            {
                if (entry._cachedEntity == _currentEntity) return;
            }

            // 取得 GlobalObjectId（只在加入時呼叫一次）
            var id = GlobalObjectId.GetGlobalObjectIdSlow(_currentEntity);
            _favoriteEntries.Add(new QuickAccessEntry
            {
                _globalIdString = id.ToString(),
                _displayName = _currentEntity.name,
                _cachedEntity = _currentEntity
            });
            SaveFavorites();
        }

        private bool HasCurrentEntity => _currentEntity != null;

        // === History & Favorites ===
        [PropertyOrder(5)]
        [Title("快速存取")]
        [OnInspectorGUI(nameof(DrawHistoryAndFavorites))]
        [ShowInInspector]
        [DisplayAsString]
        [HideLabel]
        private string _historyPlaceholder = "";

        private const int MaxHistoryCount = 10;
        private const string FavoritesPrefsKey = "FSMMonitor_Favorites";
        private const string FavoriteNamesPrefsKey = "FSMMonitor_FavoriteNames";

        // 快取結構
        public class QuickAccessEntry
        {
            public string _globalIdString;
            public string _displayName;
            public MonoEntity _cachedEntity; // 快取的引用，避免每 frame 解析
        }

        private List<QuickAccessEntry> _historyEntries = new();
        private List<QuickAccessEntry> _favoriteEntries = new();
        private bool _showHistory = true;
        private bool _showFavorites = true;

        private void DrawHistoryAndFavorites()
        {
            // 刷新按鈕
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 刷新引用", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                RefreshQuickAccessReferences();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Favorites
            EditorGUILayout.BeginHorizontal();
            _showFavorites =
                EditorGUILayout.Foldout(_showFavorites, $"★ 最愛 ({_favoriteEntries.Count})", true);
            EditorGUILayout.EndHorizontal();

            if (_showFavorites && _favoriteEntries.Count > 0)
            {
                EditorGUI.indentLevel++;
                for (int i = _favoriteEntries.Count - 1; i >= 0; i--)
                {
                    if (DrawQuickAccessItem(_favoriteEntries[i]))
                    {
                        _favoriteEntries.RemoveAt(i);
                        SaveFavorites();
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // History
            EditorGUILayout.BeginHorizontal();
            _showHistory =
                EditorGUILayout.Foldout(_showHistory, $"📋 歷史 ({_historyEntries.Count})", true);
            if (_historyEntries.Count > 0 && GUILayout.Button("清除", GUILayout.Width(50)))
            {
                _historyEntries.Clear();
            }

            EditorGUILayout.EndHorizontal();

            if (_showHistory && _historyEntries.Count > 0)
            {
                EditorGUI.indentLevel++;
                for (int i = _historyEntries.Count - 1; i >= 0; i--)
                {
                    if (DrawQuickAccessItem(_historyEntries[i]))
                    {
                        _historyEntries.RemoveAt(i);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 繪製快速存取項目，回傳 true 表示要移除
        /// </summary>
        private bool DrawQuickAccessItem(QuickAccessEntry entry)
        {
            bool shouldRemove = false;
            EditorGUILayout.BeginHorizontal();

            // 使用快取的引用，不每 frame 解析
            var isValid = entry._cachedEntity != null;
            var isCurrent = entry._cachedEntity == _currentEntity;

            // 顏色標記
            var originalColor = GUI.backgroundColor;
            if (isCurrent) GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            else if (!isValid) GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);

            // 名稱按鈕
            var buttonLabel = isValid ? entry._displayName : $"(?) {entry._displayName}";
            if (isCurrent) buttonLabel += " ◀";

            GUI.enabled = isValid;
            if (GUILayout.Button(buttonLabel, EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
            {
                if (entry._cachedEntity != null)
                {
                    SelectEntity(entry._cachedEntity);
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = originalColor;

            // 移除按鈕
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                shouldRemove = true;
            }

            EditorGUILayout.EndHorizontal();
            return shouldRemove;
        }

        /// <summary>
        /// 手動刷新所有快速存取項目的引用（解析 GlobalObjectId）
        /// </summary>
        private void RefreshQuickAccessReferences()
        {
            foreach (var entry in _favoriteEntries)
            {
                if (GlobalObjectId.TryParse(entry._globalIdString, out var globalId))
                {
                    entry._cachedEntity =
                        GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as MonoEntity;
                    if (entry._cachedEntity != null)
                    {
                        entry._displayName = entry._cachedEntity.name; // 更新名稱
                    }
                }
            }

            foreach (var entry in _historyEntries)
            {
                if (GlobalObjectId.TryParse(entry._globalIdString, out var globalId))
                {
                    entry._cachedEntity =
                        GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as MonoEntity;
                    if (entry._cachedEntity != null)
                    {
                        entry._displayName = entry._cachedEntity.name;
                    }
                }
            }

            Debug.Log("[FSM Monitor] 已刷新快速存取引用");
        }

        private void AddToHistory(MonoEntity entity)
        {
            if (entity == null) return;

            // 檢查是否已存在（用快取的引用比對）
            for (int i = _historyEntries.Count - 1; i >= 0; i--)
            {
                if (_historyEntries[i]._cachedEntity == entity)
                {
                    // 移到最後
                    var entry = _historyEntries[i];
                    _historyEntries.RemoveAt(i);
                    _historyEntries.Add(entry);
                    return;
                }
            }

            // 取得 GlobalObjectId（只在加入時呼叫一次）
            var id = GlobalObjectId.GetGlobalObjectIdSlow(entity);
            _historyEntries.Add(new QuickAccessEntry
            {
                _globalIdString = id.ToString(),
                _displayName = entity.name,
                _cachedEntity = entity
            });

            // 限制數量
            while (_historyEntries.Count > MaxHistoryCount)
            {
                _historyEntries.RemoveAt(0);
            }
        }

        private void SaveFavorites()
        {
            var ids = new List<string>();
            var names = new List<string>();
            foreach (var entry in _favoriteEntries)
            {
                ids.Add(entry._globalIdString);
                names.Add(entry._displayName);
            }

            EditorPrefs.SetString(FavoritesPrefsKey, string.Join("|", ids));
            EditorPrefs.SetString(FavoriteNamesPrefsKey, string.Join("|", names));
        }

        private void LoadFavorites()
        {
            _favoriteEntries.Clear();

            var favStr = EditorPrefs.GetString(FavoritesPrefsKey, "");
            var nameStr = EditorPrefs.GetString(FavoriteNamesPrefsKey, "");

            if (string.IsNullOrEmpty(favStr)) return;

            var ids = favStr.Split('|');
            var names = nameStr.Split('|');

            for (int i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrEmpty(ids[i])) continue;

                _favoriteEntries.Add(new QuickAccessEntry
                {
                    _globalIdString = ids[i],
                    _displayName = i < names.Length ? names[i] : "(Unknown)",
                    _cachedEntity = null // 稍後手動刷新
                });
            }

            // 載入後自動刷新一次引用
            RefreshQuickAccessReferences();
        }

        // === 階層樹 ===
        [PropertyOrder(20)]
        [Title("階層樹")]
        [OnInspectorGUI(nameof(DrawHierarchyTree))]
        [ShowInInspector]
        [DisplayAsString]
        [HideLabel]
        private string _hierarchyTreePlaceholder = "";

        private MonoObj _owningMonoObj;
        private List<HierarchyNode> _hierarchyNodes = new();

        public enum NodeType { MonoObj, MonoEntity, ModulePack }

        public class HierarchyNode
        {
            public string _name;
            public int _depth;
            public NodeType _type;
            public UnityEngine.Object _target;
            public bool _isCurrent;
            public bool _hasFsm;
            public bool _isLast; // 是否是同層最後一個
        }

        private void DrawHierarchyTree()
        {
            if (_owningMonoObj == null)
            {
                EditorGUILayout.HelpBox("未找到 MonoObj", MessageType.Info);
                return;
            }

            // 不使用 ScrollView，讓內容自然展開，整個視窗統一滾動
            // 使用 for 迴圈避免 "Collection was modified" 錯誤
            var nodeCount = _hierarchyNodes.Count;
            for (int i = 0; i < nodeCount && i < _hierarchyNodes.Count; i++)
            {
                DrawTreeNode(_hierarchyNodes[i]);
            }
        }

        private void DrawTreeNode(HierarchyNode node)
        {
            EditorGUILayout.BeginHorizontal();

            // 縮排
            GUILayout.Space(node._depth * 20);

            // 樹狀連接線符號
            var prefix = node._isLast ? "└─" : "├─";
            if (node._depth == 0) prefix = "▼";

            // 節點類型標籤
            var typeLabel = node._type switch
            {
                NodeType.MonoObj => "[Obj]",
                NodeType.MonoEntity => "[Entity]",
                NodeType.ModulePack => "[Pack]",
                _ => ""
            };

            // 節點顏色
            var originalColor = GUI.backgroundColor;
            if (node._isCurrent)
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f); // 藍色標記當前
            }
            else if (node._type == NodeType.MonoObj)
            {
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.7f); // 淡黃色
            }

            // 節點按鈕
            var buttonText = $"{prefix} {typeLabel} {node._name}";
            if (node._isCurrent) buttonText += " ◀";
            if (node._hasFsm) buttonText += " [FSM]";

            if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                if (node._target != null)
                {
                    HandleTreeNodeClick(node);
                }
            }

            GUI.backgroundColor = originalColor;

            EditorGUILayout.EndHorizontal();
        }

        private void HandleTreeNodeClick(HierarchyNode node)
        {
            if (node._type == NodeType.MonoEntity && node._target is MonoEntity entity)
            {
                SelectEntity(entity);
            }
            else
            {
                // MonoObj 或 ModulePack: Ping
                EditorGUIUtility.PingObject(node._target);
            }
        }

        private void SelectEntity(MonoEntity entity)
        {
            if (entity == null) return;

            _currentEntity = entity;
            _currentFsm = entity.FsmLogic;
            _entityPath = GetGameObjectPath(entity.gameObject);
            RefreshEntityHierarchy();
            RefreshStates();
            RefreshVariables();
            Repaint();

            // 加入歷史紀錄
            AddToHistory(entity);

            // 同時在 Hierarchy 中 Ping 這個物件
            EditorGUIUtility.PingObject(entity);
        }

        // === 狀態列表 ===
        [PropertyOrder(10)]
        [Title("States (單擊 Ping, 雙擊選取)")]
        [ShowInInspector]
        [TableList(AlwaysExpanded = true, IsReadOnly = true)]
        private List<StateDisplayInfo> _states = new();

        // === 變數列表 ===
        [PropertyOrder(30)]
        [Title("Variables (單擊 Ping, 雙擊選取)")]
        [ShowInInspector]
        [TableList(AlwaysExpanded = true, IsReadOnly = true)]
        private List<VariableDisplayInfo> _variables = new();

        // 追蹤雙擊
        private static double _lastClickTime;
        private static object _lastClickedItem;
        private const double DoubleClickThreshold = 0.3;

        // === 內部資料結構 ===
        [Serializable]
        public class StateDisplayInfo
        {
            [TableColumnWidth(200, Resizable = true)]
            [Button("$_stateName")]
            [GUIColor(nameof(GetStateColor))]
            [PropertyOrder(0)]
            public void SelectState()
            {
                if (_stateBehaviour != null)
                {
                    HandleClick(_stateBehaviour, this);
                }
            }

            [TableColumnWidth(60)]
            [Button("跳轉")]
            [EnableIf(nameof(CanGoToState))]
            [PropertyOrder(1)]
            public void GoToState()
            {
                if (_context != null && Application.isPlaying)
                {
                    _context.RestoreState(_stateId);
                    Debug.Log($"[FSM Monitor] 跳轉到狀態: {_stateName} (ID: {_stateId})", _context);
                }
            }

            [HideInInspector] public string _stateName;
            [HideInInspector] public int _stateId;
            [HideInInspector] public bool _isActive;
            [HideInInspector] public bool _isPrevious;
            [HideInInspector] public StateMachineLogic _context;
            [HideInInspector] public MonoStateBehaviour _stateBehaviour;

            private Color GetStateColor()
            {
                if (_isActive) return new Color(0.3f, 0.85f, 0.3f); // 綠色 - Active
                if (_isPrevious) return new Color(0.95f, 0.85f, 0.3f); // 黃色 - Previous
                return Color.white;
            }

            private bool CanGoToState()
            {
                return Application.isPlaying && _context != null;
            }
        }

        [Serializable]
        public class VariableDisplayInfo
        {
            [TableColumnWidth(150)]
            [Button("$_varName")]
            [PropertyOrder(0)]
            public void SelectVariable()
            {
                if (_variable != null)
                {
                    HandleClick(_variable, this);
                }
            }

            [ShowInInspector]
            [TableColumnWidth(100)]
            [DisplayAsString]
            [PropertyOrder(1)]
            public string TypeName => _typeName;

            [ShowInInspector]
            [TableColumnWidth(120)]
            [DisplayAsString]
            [PropertyOrder(2)]
            public string CurrentValue => _currentValue;

            [HideInInspector] public string _varName;
            [HideInInspector] public string _typeName;
            [HideInInspector] public string _currentValue;
            [HideInInspector] public AbstractMonoVariable _variable;
        }

        // === 點擊處理 ===
        private static void HandleClick(UnityEngine.Object target, object clickedItem)
        {
            if (target == null) return;

            var currentTime = EditorApplication.timeSinceStartup;
            var isDoubleClick = _lastClickedItem == clickedItem &&
                                (currentTime - _lastClickTime) < DoubleClickThreshold;

            if (isDoubleClick)
            {
                // 雙擊：選取物件
                Selection.activeObject = target;
                _lastClickedItem = null;
            }
            else
            {
                // 單擊：Ping
                EditorGUIUtility.PingObject(target);
            }

            _lastClickTime = currentTime;
            _lastClickedItem = clickedItem;
        }

        // === Unity Editor 生命週期 ===
        protected override void OnEnable()
        {
            base.OnEnable();
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // 載入 Favorites
            LoadFavorites();

            // 初次嘗試從當前選擇尋找
            OnSelectionChanged();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Play Mode 切換時刷新
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                RefreshAll();
            }
        }

        private void Update()
        {
            // Play Mode 下持續更新狀態顯示
            if (Application.isPlaying && _currentFsm != null)
            {
                RefreshStates();
                Repaint();
            }
        }

        // === 核心方法 ===
        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject == null) return;

            var entity = FindEntityFromSelection(Selection.activeGameObject);
            if (entity != null && entity != _currentEntity)
            {
                _currentEntity = entity;
                _currentFsm = entity.FsmLogic;
                _entityPath = GetGameObjectPath(entity.gameObject);
                RefreshEntityHierarchy();
                RefreshStates();
                RefreshVariables();
                Repaint();
            }
        }

        private void RefreshEntityHierarchy()
        {
            _owningMonoObj = null;
            _hierarchyNodes.Clear();

            if (_currentEntity == null) return;

            // 往上找 Root MonoObj（最上層的 MonoObj）
            _owningMonoObj = FindRootMonoObj(_currentEntity.transform);

            // 建立階層樹
            if (_owningMonoObj != null)
            {
                BuildHierarchyTree(_owningMonoObj.transform, 0);
            }
        }

        /// <summary>
        /// 遞迴建立階層樹
        /// </summary>
        private void BuildHierarchyTree(Transform root, int depth)
        {
            // 檢查是否有 MonoObj
            var monoObj = root.GetComponent<MonoObj>();
            if (monoObj != null)
            {
                _hierarchyNodes.Add(new HierarchyNode
                {
                    _name = root.name,
                    _depth = depth,
                    _type = NodeType.MonoObj,
                    _target = monoObj,
                    _isCurrent = false,
                    _hasFsm = false,
                    _isLast = false
                });
                depth++;
            }

            // 收集此層級的子項目
            var childItems = new List<(Transform transform, NodeType type, UnityEngine.Object target)>();

            // 找此層的 MonoEntity（同 GameObject 或直接子層）
            var entities = root.GetComponents<MonoEntity>();
            foreach (var entity in entities)
            {
                childItems.Add((root, NodeType.MonoEntity, entity));
            }

            // 遍歷子物件
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);

                // 檢查子物件是否有 MonoObj（nested MonoObj）
                var childMonoObj = child.GetComponent<MonoObj>();
                if (childMonoObj != null)
                {
                    // 遞迴處理 nested MonoObj
                    BuildHierarchyTree(child, depth);
                    continue;
                }

                // 檢查子物件是否有 MonoEntity
                var childEntity = child.GetComponent<MonoEntity>();
                if (childEntity != null)
                {
                    childItems.Add((child, NodeType.MonoEntity, childEntity));
                    // 不遞迴進去，因為那是另一個 Entity 的範圍
                    continue;
                }

                // 檢查子物件是否有 ModulePack
                var childPack = child.GetComponent<MonoModulePack>();
                if (childPack != null)
                {
                    childItems.Add((child, NodeType.ModulePack, childPack));
                    continue;
                }

                // 其他情況：繼續往下找
                BuildHierarchyTree(child, depth);
            }

            // 添加收集到的子項目
            for (int i = 0; i < childItems.Count; i++)
            {
                var (transform, type, target) = childItems[i];
                var isLast = (i == childItems.Count - 1);
                var isCurrent = type == NodeType.MonoEntity && target == _currentEntity;
                var hasFsm = type == NodeType.MonoEntity && target is MonoEntity e && e.FsmLogic != null;

                _hierarchyNodes.Add(new HierarchyNode
                {
                    _name = target is Component c ? c.gameObject.name : transform.name,
                    _depth = depth,
                    _type = type,
                    _target = target,
                    _isCurrent = isCurrent,
                    _hasFsm = hasFsm,
                    _isLast = isLast
                });
            }
        }

        /// <summary>
        /// 往上找 Root MonoObj（最上層沒有 parent MonoObj 的那個）
        /// </summary>
        private MonoObj FindRootMonoObj(Transform startFrom)
        {
            MonoObj rootMonoObj = null;
            var current = startFrom;

            while (current != null)
            {
                var monoObj = current.GetComponent<MonoObj>();
                if (monoObj != null)
                {
                    rootMonoObj = monoObj;
                }
                current = current.parent;
            }

            return rootMonoObj;
        }

        /// <summary>
        /// 先往上找 MonoEntity，找到後就可以從 Entity 取得 FSM 和 VariableFolder
        /// </summary>
        private MonoEntity FindEntityFromSelection(GameObject selected)
        {
            // 先檢查自己
            var entity = selected.GetComponent<MonoEntity>();
            if (entity != null) return entity;

            // 往上遍歷找 MonoEntity
            var current = selected.transform;
            while (current != null)
            {
                entity = current.GetComponent<MonoEntity>();
                if (entity != null) return entity;
                current = current.parent;
            }

            return null;
        }

        private void RefreshStates()
        {
            _states.Clear();

            if (_currentEntity == null) return;

            if (Application.isPlaying)
            {
                // Play Mode：使用 FSMLogic 的 StateMachines
                RefreshStatesFromFsmLogic();
            }
            else
            {
                // Edit Mode：使用 StateFolder
                RefreshStatesFromStateFolder();
            }
        }

        private void RefreshStatesFromFsmLogic()
        {
            if (_currentFsm == null) return;
            if (_currentFsm.StateMachines == null || _currentFsm.StateMachines.Count == 0) return;

            foreach (var stateMachine in _currentFsm.StateMachines)
            {
                if (stateMachine.States == null) continue;

                var activeState = stateMachine.ActiveState;
                var previousState = stateMachine.PreviousState;

                foreach (var state in stateMachine.States)
                {
                    if (state == null) continue;

                    var isActive = activeState != null && activeState == state;
                    var isPrevious = !isActive && previousState != null && previousState == state;

                    // 根據狀態添加後綴
                    var displayName = state.Name ?? "(Unnamed)";
                    if (isActive) displayName += " [Active]";
                    else if (isPrevious) displayName += " [Last]";

                    // 嘗試取得 MonoStateBehaviour 引用
                    var stateBehaviour = state as MonoStateBehaviour;

                    _states.Add(new StateDisplayInfo
                    {
                        _stateName = displayName,
                        _stateId = state.StateId,
                        _isActive = isActive,
                        _isPrevious = isPrevious,
                        _context = _currentFsm,
                        _stateBehaviour = stateBehaviour
                    });
                }
            }
        }

        private void RefreshStatesFromStateFolder()
        {
            var stateFolder = _currentEntity.StateFolder;
            if (stateFolder == null) return;

            var allStates = stateFolder.GetComponentsInChildren<MonoStateBehaviour>(true);

            foreach (var stateBehaviour in allStates)
            {
                if (stateBehaviour == null) continue;

                var displayName = stateBehaviour.Name ?? stateBehaviour.gameObject.name;

                _states.Add(new StateDisplayInfo
                {
                    _stateName = displayName,
                    _stateId = stateBehaviour.StateId,
                    _isActive = false,
                    _isPrevious = false,
                    _context = _currentFsm,
                    _stateBehaviour = stateBehaviour
                });
            }
        }

        private void RefreshVariables()
        {
            _variables.Clear();

            if (_currentEntity == null) return;

            // 從 MonoEntity 取得 VariableFolder
            var variableFolder = _currentEntity.VariableFolder;
            if (variableFolder == null) return;

            // 取得所有變數
            var allVariables = variableFolder.GetComponentsInChildren<AbstractMonoVariable>(true);

            foreach (var variable in allVariables)
            {
                if (variable == null) continue;

                _variables.Add(new VariableDisplayInfo
                {
                    _varName = variable.name,
                    _typeName = variable.GetType().Name,
                    _currentValue = GetVariableValueString(variable),
                    _variable = variable
                });
            }
        }

        private string GetVariableValueString(AbstractMonoVariable variable)
        {
            if (variable == null) return "null";

            try
            {
#if UNITY_EDITOR
                // 使用 StringValue 屬性（在 Editor 下可用）
                return variable.StringValue ?? "null";
#else
                return "N/A";
#endif
            }
            catch
            {
                return "(Error)";
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";

            var path = obj.name;
            var current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
