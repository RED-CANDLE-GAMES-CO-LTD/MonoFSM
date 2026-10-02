using System;
using System.Collections.Generic;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MonoFSM.Editor.GameDataTable
{
    /// <summary>
    ///     GameData 總表：把專案裡所有 GameData asset 攤成一張表，一列一顆 asset，欄 = 篩選結果裡出現過的
    ///     config VariableTag 聯集（含 _baseConfig 疊層繼承來的），格子可直接改值。
    ///     單一 asset 的疊層細節看 GameData Inspector 上的「Config 疊層總覽」；這裡是跨 asset 比數值用。
    ///     格子三種狀態：本層 override（正常字、可改）／繼承自 base（灰斜體，改值＝在本層建 override）／
    ///     沒有這個 key（淡色「—」，點一下新增）。讀寫一律走 GameData 的 internal editor helper（有 Undo）。
    ///     入口：Tools/MonoFSM/GameData 總表
    /// </summary>
    public class GameDataTableWindow : OdinEditorWindow
    {
        private const string LogTag = "[GameDataTable]";
        private const float RowHeight = 22f;
        private const float CellPadding = 2f;
        private const float DefaultTagColumnWidth = 96f;
        private const float ScrollWheelStep = 20f;

        private const string PriceKey = "price";
        private const string FloatKeyPrefix = "f:";
        private const string ObjKeyPrefix = "o:";
        private const string DataFunctionsFieldName = "_dataFunctions";
        private const string BasePriceFieldName = "_basePrice";

        private const int FixedColName = 0;
        private const int FixedColType = 1;
        private const int FixedColBase = 2;
        private const int FixedColCount = 3;

        [MenuItem("Tools/MonoFSM/GameData 總表")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameDataTableWindow>();
            window.titleContent = new GUIContent("GameData 總表");
            window.minSize = new Vector2(640, 300);
            window.Show();
        }

        #region 記住的視窗狀態（window serialized field，跟著 layout 存）

        [SerializeField] private string _search = "";
        [SerializeField] private string _typeFilter = "";
        [SerializeField] private string _folderFilter = "";
        [SerializeField] private bool _showObjColumns;
        [SerializeField] private List<string> _hiddenColumnKeys = new();
        [SerializeField] private List<string> _widthKeys = new();
        [SerializeField] private List<float> _widthValues = new();
        [SerializeField] private string _scrollSortKey = "";
        [SerializeField] private bool _scrollSortAscending = true;
        [SerializeField] private MultiColumnHeaderState _fixedHeaderState;
        [SerializeField] private Vector2 _scroll;

        #endregion

        #region runtime cache（不序列化）

        private enum ColumnKind
        {
            Price,
            FloatTag,
            ObjTag,
        }

        /// <summary>可捲動區的一欄：種類 + tag + 持久化用的 key（tag 用 asset guid，改名不影響）。</summary>
        private readonly struct ColumnDef
        {
            public readonly ColumnKind Kind;
            public readonly VariableTag Tag;
            public readonly string Key;

            public ColumnDef(ColumnKind kind, VariableTag tag, string key)
            {
                Kind = kind;
                Tag = tag;
                Key = key;
            }
        }

        /// <summary>表格的一列：一顆 GameData asset。名稱 / 型別 / 路徑在掃描時算好，數值一律即時從 asset 讀。</summary>
        private sealed class Row
        {
            public GameData Data;
            public string Path;
            public string Folder;
            public string TypeName;
            public GUIContent NameContent;
            public GUIContent TypeContent;

            public string SortText;
            public float SortValue;
            public bool SortMissing;
        }

        private readonly List<Row> _allRows = new();
        private readonly List<Row> _visibleRows = new();
        private readonly List<ColumnDef> _scrollColumns = new();
        private readonly List<VariableTag> _floatTagUnion = new();
        private readonly List<VariableTag> _objTagUnion = new();
        private readonly List<VariableTag> _tagBuffer = new();
        private readonly HashSet<VariableTag> _tagSet = new();
        private readonly HashSet<string> _hiddenSet = new();
        private readonly Dictionary<VariableTag, string> _tagGuidCache = new();
        private readonly List<string> _typeOptions = new();
        private readonly List<string> _folderOptions = new();
        private string[] _searchTokens = Array.Empty<string>();
        private bool _anyPriceInFilter;

        private MultiColumnHeader _fixedHeader;
        private MultiColumnHeader _scrollHeader;
        private SearchField _searchField;

        private bool _needsRescan = true;
        private bool _needsRefresh = true;

        private readonly GUIContent _tmpContent = new();
        private readonly GUIContent _missingContent = new("—");
        private GUIContent _saveContent = new("儲存");
        private int _lastDirtyCount = -1;

        #endregion

        #region styles

        private GUIStyle _ownStyle;
        private GUIStyle _inheritedStyle;
        private GUIStyle _missingStyle;
        private GUIStyle _linkStyle;
        private GUIStyle _dimLabelStyle;
        private GUIStyle _legendTextStyle;
        private GUIStyle _objOwnStyle;
        private GUIStyle _objInheritedStyle;

        private static Color ZebraColor =>
            EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.05f);

        private static Color SelectedRowColor =>
            EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.9f, 0.25f) : new Color(0.24f, 0.48f, 0.9f, 0.18f);

        private static Color InheritedTextColor =>
            EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.42f, 0.42f, 0.42f);

        private static Color MissingTextColor =>
            EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.22f) : new Color(0f, 0f, 0f, 0.25f);

        private void EnsureStyles()
        {
            if (_ownStyle != null)
                return;

            _ownStyle = new GUIStyle(EditorStyles.numberField)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 12,
            };

            _inheritedStyle = new GUIStyle(_ownStyle) { fontStyle = FontStyle.Italic };
            SetAllTextColors(_inheritedStyle, InheritedTextColor);

            _missingStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            SetAllTextColors(_missingStyle, MissingTextColor);
            _missingStyle.hover.textColor = InheritedTextColor;

            _linkStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _linkStyle.hover.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.5f, 0.75f, 1f)
                : new Color(0.1f, 0.35f, 0.8f);

            _dimLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            SetAllTextColors(_dimLabelStyle, InheritedTextColor);

            _legendTextStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };

            _objOwnStyle = new GUIStyle(_linkStyle) { alignment = TextAnchor.MiddleRight };
            _objInheritedStyle = new GUIStyle(_objOwnStyle) { fontStyle = FontStyle.Italic };
            SetAllTextColors(_objInheritedStyle, InheritedTextColor);
        }

        private static void SetAllTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onFocused.textColor = color;
        }

        #endregion

        #region lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += OnUndoRedo;
            _hiddenSet.Clear();
            for (var i = 0; i < _hiddenColumnKeys.Count; i++)
                _hiddenSet.Add(_hiddenColumnKeys[i]);
            _needsRescan = true;
        }

        protected override void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            base.OnDestroy();
        }

        private void OnProjectChange()
        {
            _needsRescan = true;
            Repaint();
        }

        private void OnUndoRedo()
        {
            //undo 可能把 key 加回來 / 拿掉，欄位聯集要重算
            _needsRefresh = true;
            Repaint();
        }

        #endregion

        #region 掃描 / 篩選 / 排序

        private void Rescan()
        {
            _needsRescan = false;
            _allRows.Clear();
            _typeOptions.Clear();
            _folderOptions.Clear();

            var typeSet = new HashSet<string>();
            var folderSet = new HashSet<string>();
            var guids = AssetDatabase.FindAssets("t:GameData");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var data = AssetDatabase.LoadAssetAtPath<GameData>(path);
                if (data == null)
                    continue;

                var slash = path.LastIndexOf('/');
                var folder = slash > 0 ? path.Substring(0, slash) : "";
                var typeName = data.GetType().Name;
                _allRows.Add(new Row
                {
                    Data = data,
                    Path = path,
                    Folder = folder,
                    TypeName = typeName,
                    NameContent = new GUIContent(data.name, path),
                    TypeContent = new GUIContent(typeName, data.GetType().FullName),
                });
                if (typeSet.Add(typeName))
                    _typeOptions.Add(typeName);
                if (folderSet.Add(folder))
                    _folderOptions.Add(folder);
            }

            _typeOptions.Sort(StringComparer.Ordinal);
            _folderOptions.Sort(StringComparer.Ordinal);
            _needsRefresh = true;
        }

        private void RefreshView()
        {
            _needsRefresh = false;
            CaptureScrollColumnWidths();

            _searchTokens = string.IsNullOrWhiteSpace(_search)
                ? Array.Empty<string>()
                : _search.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            _visibleRows.Clear();
            _floatTagUnion.Clear();
            _objTagUnion.Clear();
            _anyPriceInFilter = false;

            _tagSet.Clear();
            for (var i = 0; i < _allRows.Count; i++)
            {
                var row = _allRows[i];
                if (row.Data == null || !PassFilter(row))
                    continue;
                _visibleRows.Add(row);

                _tagBuffer.Clear();
                row.Data.CollectConfigTags(_tagBuffer);
                for (var t = 0; t < _tagBuffer.Count; t++)
                    if (_tagBuffer[t] != null && _tagSet.Add(_tagBuffer[t]))
                        _floatTagUnion.Add(_tagBuffer[t]);

                if (!_anyPriceInFilter && FindPriceIndex(row.Data) >= 0)
                    _anyPriceInFilter = true;
            }

            if (_showObjColumns)
            {
                _tagSet.Clear();
                for (var i = 0; i < _visibleRows.Count; i++)
                {
                    _tagBuffer.Clear();
                    _visibleRows[i].Data.CollectObjConfigTags(_tagBuffer);
                    for (var t = 0; t < _tagBuffer.Count; t++)
                        if (_tagBuffer[t] != null && _tagSet.Add(_tagBuffer[t]))
                            _objTagUnion.Add(_tagBuffer[t]);
                }
            }

            _floatTagUnion.Sort(CompareTagName);
            _objTagUnion.Sort(CompareTagName);

            BuildScrollHeader();
            ApplySort();
        }

        private bool PassFilter(Row row)
        {
            if (!string.IsNullOrEmpty(_typeFilter) && row.TypeName != _typeFilter)
                return false;

            if (!string.IsNullOrEmpty(_folderFilter)
                && row.Folder != _folderFilter
                && !row.Folder.StartsWith(_folderFilter + "/", StringComparison.Ordinal))
                return false;

            //空白分隔的每個字都要出現在名稱或型別裡（不分大小寫）
            for (var i = 0; i < _searchTokens.Length; i++)
            {
                var token = _searchTokens[i];
                if (row.Data.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0
                    && row.TypeName.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            return true;
        }

        private static int CompareTagName(VariableTag a, VariableTag b)
        {
            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplySort()
        {
            var fixedIndex = _fixedHeader != null ? _fixedHeader.sortedColumnIndex : -1;
            if (fixedIndex >= 0)
            {
                var ascending = _fixedHeader.IsSortedAscending(fixedIndex);
                for (var i = 0; i < _visibleRows.Count; i++)
                {
                    var row = _visibleRows[i];
                    row.SortText = fixedIndex switch
                    {
                        FixedColName => row.Data.name,
                        FixedColType => row.TypeName,
                        _ => row.Data.EditorBaseConfig != null ? row.Data.EditorBaseConfig.name : null,
                    };
                }

                _visibleRows.Sort((a, b) => CompareText(a, b, ascending));
                return;
            }

            var scrollIndex = FindScrollColumnIndex(_scrollSortKey);
            if (scrollIndex >= 0)
            {
                var column = _scrollColumns[scrollIndex];
                for (var i = 0; i < _visibleRows.Count; i++)
                    FillSortValue(_visibleRows[i], column);

                var ascending = _scrollSortAscending;
                if (column.Kind == ColumnKind.ObjTag)
                    _visibleRows.Sort((a, b) => CompareText(a, b, ascending));
                else
                    _visibleRows.Sort((a, b) => CompareValue(a, b, ascending));
                return;
            }

            //沒選排序：照路徑，同資料夾的會排在一起
            _visibleRows.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
        }

        private static void FillSortValue(Row row, ColumnDef column)
        {
            row.SortMissing = false;
            row.SortValue = 0f;
            row.SortText = null;
            switch (column.Kind)
            {
                case ColumnKind.Price:
                {
                    var index = FindPriceIndex(row.Data);
                    row.SortMissing = index < 0;
                    if (index >= 0)
                        row.SortValue = ((PriceData)row.Data.DataFunctions[index]).BasePrice;
                    break;
                }
                case ColumnKind.FloatTag:
                    if (row.Data.EditorTryGetOwnConfig(column.Tag, out var own))
                        row.SortValue = own;
                    else if (row.Data.EditorFindBaseFloatSource(column.Tag, out var inherited) != null)
                        row.SortValue = inherited;
                    else
                        row.SortMissing = true;
                    break;
                case ColumnKind.ObjTag:
                    if (row.Data.EditorTryGetOwnObjConfig(column.Tag, out var obj)
                        || row.Data.EditorFindBaseObjSource(column.Tag, out obj) != null)
                        row.SortText = obj != null ? obj.name : "null";
                    break;
            }
        }

        //沒值的一律排最後，不管升降冪
        private static int CompareValue(Row a, Row b, bool ascending)
        {
            if (a.SortMissing != b.SortMissing)
                return a.SortMissing ? 1 : -1;
            var result = a.SortValue.CompareTo(b.SortValue);
            if (result == 0)
                return string.CompareOrdinal(a.Path, b.Path);
            return ascending ? result : -result;
        }

        private static int CompareText(Row a, Row b, bool ascending)
        {
            var aMissing = string.IsNullOrEmpty(a.SortText);
            var bMissing = string.IsNullOrEmpty(b.SortText);
            if (aMissing != bMissing)
                return aMissing ? 1 : -1;
            var result = string.Compare(a.SortText, b.SortText, StringComparison.OrdinalIgnoreCase);
            if (result == 0)
                return string.CompareOrdinal(a.Path, b.Path);
            return ascending ? result : -result;
        }

        private static int FindPriceIndex(GameData data)
        {
            var functions = data.DataFunctions;
            if (functions == null)
                return -1;
            for (var i = 0; i < functions.Length; i++)
                if (functions[i] is PriceData)
                    return i;
            return -1;
        }

        #endregion

        #region header

        private static MultiColumnHeaderState.Column MakeColumn(string title, string tooltip, float width)
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(title, tooltip),
                width = width,
                minWidth = 40f,
                autoResize = false,
                allowToggleVisibility = false,
                canSort = true,
                headerTextAlignment = TextAlignment.Left,
                sortingArrowAlignment = TextAlignment.Right,
            };
        }

        private void EnsureFixedHeader()
        {
            if (_fixedHeaderState == null
                || _fixedHeaderState.columns == null
                || _fixedHeaderState.columns.Length != FixedColCount)
            {
                _fixedHeaderState = new MultiColumnHeaderState(new[]
                {
                    MakeColumn("名稱", "GameData asset（點一下 ping / 選取）", 210f),
                    MakeColumn("型別", "GameData 子類別", 110f),
                    MakeColumn("BaseConfig", "_baseConfig：自己查不到的 key 往這裡繼承（點一下 ping）", 150f),
                });
                _fixedHeader = null;
            }

            if (_fixedHeader != null)
                return;
            _fixedHeader = new MultiColumnHeader(_fixedHeaderState);
            _fixedHeader.sortingChanged += OnFixedSortingChanged;
        }

        private void BuildScrollHeader()
        {
            _scrollColumns.Clear();
            var columns = new List<MultiColumnHeaderState.Column>();

            if (_anyPriceInFilter && !_hiddenSet.Contains(PriceKey))
            {
                _scrollColumns.Add(new ColumnDef(ColumnKind.Price, null, PriceKey));
                columns.Add(MakeColumn(
                    "PriceData 售價",
                    "_dataFunctions 裡 PriceData._basePrice（GameData.Price / GetPriceFromGameData 讀這個）。\n"
                    + "訂購終端機讀的是 d_Price config 欄，兩者不同步",
                    GetSavedWidth(PriceKey)));
            }

            for (var i = 0; i < _floatTagUnion.Count; i++)
            {
                var tag = _floatTagUnion[i];
                var key = FloatKeyPrefix + GetTagGuid(tag);
                if (_hiddenSet.Contains(key))
                    continue;
                _scrollColumns.Add(new ColumnDef(ColumnKind.FloatTag, tag, key));
                columns.Add(MakeColumn(tag.name, tag.name, GetSavedWidth(key)));
            }

            if (_showObjColumns)
                for (var i = 0; i < _objTagUnion.Count; i++)
                {
                    var tag = _objTagUnion[i];
                    var key = ObjKeyPrefix + GetTagGuid(tag);
                    if (_hiddenSet.Contains(key))
                        continue;
                    _scrollColumns.Add(new ColumnDef(ColumnKind.ObjTag, tag, key));
                    columns.Add(MakeColumn("◆ " + tag.name, tag.name + "（MonoObj config，唯讀）", GetSavedWidth(key)));
                }

            if (columns.Count == 0)
            {
                _scrollHeader = null;
                return;
            }

            var state = new MultiColumnHeaderState(columns.ToArray());
            var sortIndex = FindScrollColumnIndex(_scrollSortKey);
            if (sortIndex >= 0)
            {
                state.sortedColumnIndex = sortIndex;
                state.columns[sortIndex].sortedAscending = _scrollSortAscending;
            }

            _scrollHeader = new MultiColumnHeader(state);
            _scrollHeader.sortingChanged += OnScrollSortingChanged;
        }

        private int FindScrollColumnIndex(string key)
        {
            if (string.IsNullOrEmpty(key))
                return -1;
            for (var i = 0; i < _scrollColumns.Count; i++)
                if (_scrollColumns[i].Key == key)
                    return i;
            return -1;
        }

        private void OnFixedSortingChanged(MultiColumnHeader header)
        {
            if (_scrollHeader != null)
                _scrollHeader.state.sortedColumnIndex = -1;
            _scrollSortKey = "";
            ApplySort();
        }

        private void OnScrollSortingChanged(MultiColumnHeader header)
        {
            _fixedHeader.state.sortedColumnIndex = -1;
            var index = header.sortedColumnIndex;
            _scrollSortKey = index >= 0 && index < _scrollColumns.Count ? _scrollColumns[index].Key : "";
            _scrollSortAscending = index < 0 || header.IsSortedAscending(index);
            ApplySort();
        }

        private string GetTagGuid(VariableTag tag)
        {
            if (_tagGuidCache.TryGetValue(tag, out var guid))
                return guid;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tag, out guid, out long _))
                guid = tag.name; //不是 asset（理論上不會發生）就退回名字當 key
            _tagGuidCache[tag] = guid;
            return guid;
        }

        private float GetSavedWidth(string key)
        {
            var index = _widthKeys.IndexOf(key);
            return index >= 0 && index < _widthValues.Count ? _widthValues[index] : DefaultTagColumnWidth;
        }

        private void CaptureScrollColumnWidths()
        {
            if (_scrollHeader == null)
                return;
            var columns = _scrollHeader.state.columns;
            for (var i = 0; i < columns.Length && i < _scrollColumns.Count; i++)
            {
                var key = _scrollColumns[i].Key;
                var index = _widthKeys.IndexOf(key);
                if (index >= 0 && index < _widthValues.Count)
                {
                    _widthValues[index] = columns[i].width;
                }
                else
                {
                    _widthKeys.Add(key);
                    _widthValues.Add(columns[i].width);
                }
            }
        }

        private void SetColumnHidden(string key, bool hidden)
        {
            if (hidden)
            {
                if (_hiddenSet.Add(key))
                    _hiddenColumnKeys.Add(key);
            }
            else if (_hiddenSet.Remove(key))
            {
                _hiddenColumnKeys.Remove(key);
            }

            _needsRefresh = true;
            Repaint();
        }

        #endregion

        #region OnGUI

        protected override void OnImGUI()
        {
            //不呼叫 base：這個視窗沒有要給 Odin 畫的欄位，整張表自己畫
            EnsureStyles();
            EnsureFixedHeader();

            var evt = Event.current;
            if (evt.type == EventType.Layout)
            {
                if (_needsRescan)
                    Rescan();
                if (_needsRefresh)
                    RefreshView();
            }

            DrawToolbar();
            DrawLegendBar();

            var tableRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            //表格本體全用 rect 畫、而且排在最後，Layout pass 跳過不影響前面控制項的 control id
            if (evt.type == EventType.Layout)
                return;
            DrawTable(tableRect);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            _searchField ??= new SearchField();
            EditorGUI.BeginChangeCheck();
            _search = _searchField.OnToolbarGUI(_search ?? "", GUILayout.MinWidth(120f), GUILayout.MaxWidth(260f));
            if (EditorGUI.EndChangeCheck())
                _needsRefresh = true;

            _tmpContent.text = string.IsNullOrEmpty(_typeFilter) ? "型別：全部" : "型別：" + _typeFilter;
            _tmpContent.tooltip = "只看某個 GameData 子類別";
            if (EditorGUILayout.DropdownButton(_tmpContent, FocusType.Passive, EditorStyles.toolbarDropDown,
                    GUILayout.MaxWidth(170f)))
                ShowTypeMenu();

            _tmpContent.text = string.IsNullOrEmpty(_folderFilter) ? "資料夾：全部" : "資料夾：" + ShortFolder(_folderFilter);
            _tmpContent.tooltip = "只看某個資料夾（含子資料夾）";
            if (EditorGUILayout.DropdownButton(_tmpContent, FocusType.Passive, EditorStyles.toolbarDropDown,
                    GUILayout.MaxWidth(220f)))
                ShowFolderMenu();

            _tmpContent.text = "欄位";
            _tmpContent.tooltip = "勾選要顯示哪些 tag 欄";
            if (EditorGUILayout.DropdownButton(_tmpContent, FocusType.Passive, EditorStyles.toolbarDropDown,
                    GUILayout.Width(52f)))
                ShowColumnMenu();

            EditorGUI.BeginChangeCheck();
            _tmpContent.text = "Obj 欄";
            _tmpContent.tooltip = "顯示 MonoObj config（_objConfigs）欄，唯讀，點格子 ping 指到的 prefab";
            _showObjColumns = GUILayout.Toggle(_showObjColumns, _tmpContent, EditorStyles.toolbarButton,
                GUILayout.Width(52f));
            if (EditorGUI.EndChangeCheck())
                _needsRefresh = true;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("重新掃描", EditorStyles.toolbarButton, GUILayout.Width(64f)))
            {
                _needsRescan = true;
                Repaint();
            }

            var dirtyCount = CountDirty();
            if (dirtyCount != _lastDirtyCount)
            {
                _lastDirtyCount = dirtyCount;
                _saveContent = new GUIContent(
                    dirtyCount > 0 ? $"儲存 ({dirtyCount})" : "儲存",
                    "AssetDatabase.SaveAssets()");
            }

            using (new EditorGUI.DisabledScope(dirtyCount == 0))
                if (GUILayout.Button(_saveContent, EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    AssetDatabase.SaveAssets();

            GUILayout.EndHorizontal();
        }

        private void DrawLegendBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("圖例", _legendTextStyle, GUILayout.Width(28f));

            DrawLegendSample("1.5", _ownStyle, "本層 override（可改；右鍵還原）");
            DrawLegendSample("1.5", _inheritedStyle, "繼承自 base（滑過看來源；改值＝在本層建 override）");
            DrawLegendSample("—", _missingStyle, "沒有這個 key（點一下新增）");

            GUILayout.FlexibleSpace();
            _tmpContent.text = _visibleRows.Count + " / " + _allRows.Count + " 筆";
            _tmpContent.tooltip = "";
            GUILayout.Label(_tmpContent, _legendTextStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawLegendSample(string sample, GUIStyle sampleStyle, string description)
        {
            var rect = GUILayoutUtility.GetRect(36f, 36f, 16f, 16f, GUILayout.Width(36f));
            rect.y += 1f;
            if (Event.current.type == EventType.Repaint)
            {
                _tmpContent.text = sample;
                _tmpContent.tooltip = "";
                sampleStyle.Draw(rect, _tmpContent, false, false, false, false);
            }

            GUILayout.Label(description, _legendTextStyle);
            GUILayout.Space(8f);
        }

        private int CountDirty()
        {
            var count = 0;
            for (var i = 0; i < _allRows.Count; i++)
                if (_allRows[i].Data != null && EditorUtility.IsDirty(_allRows[i].Data))
                    count++;
            return count;
        }

        private void DrawTable(Rect rect)
        {
            var evt = Event.current;
            var fixedWidth = _fixedHeader.state.widthOfAllVisibleColumns;
            var headerHeight = _fixedHeader.height;
            var rightX = rect.x + fixedWidth;
            var rightWidth = Mathf.Max(0f, rect.width - fixedWidth);

            _fixedHeader.OnGUI(new Rect(rect.x, rect.y, fixedWidth, headerHeight), 0f);
            var rightHeaderRect = new Rect(rightX, rect.y, rightWidth, headerHeight);
            if (_scrollHeader != null)
            {
                _scrollHeader.OnGUI(rightHeaderRect, _scroll.x);
            }
            else
            {
                _tmpContent.text = "（篩選結果裡沒有 config 欄）";
                _tmpContent.tooltip = "";
                GUI.Label(rightHeaderRect, _tmpContent, _dimLabelStyle);
            }

            var bodyY = rect.y + headerHeight;
            var bodyHeight = Mathf.Max(0f, rect.height - headerHeight);
            var contentWidth = _scrollHeader != null ? _scrollHeader.state.widthOfAllVisibleColumns : 0f;
            var contentHeight = _visibleRows.Count * RowHeight;

            //左側固定欄沒有自己的捲軸，滾輪轉給右邊的 scroll view
            var leftBody = new Rect(rect.x, bodyY, fixedWidth, bodyHeight);
            if (evt.type == EventType.ScrollWheel && leftBody.Contains(evt.mousePosition))
            {
                var viewHeight = bodyHeight
                                 - (contentWidth > rightWidth ? GUI.skin.horizontalScrollbar.fixedHeight : 0f);
                _scroll.y = Mathf.Clamp(
                    _scroll.y + evt.delta.y * ScrollWheelStep,
                    0f,
                    Mathf.Max(0f, contentHeight - viewHeight));
                evt.Use();
                Repaint();
            }

            var first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / RowHeight));
            var last = Mathf.Min(_visibleRows.Count - 1, Mathf.CeilToInt((_scroll.y + bodyHeight) / RowHeight));

            GUI.BeginClip(leftBody);
            for (var i = first; i <= last; i++)
            {
                var rowRect = new Rect(0f, i * RowHeight - _scroll.y, fixedWidth, RowHeight);
                DrawRowBackground(rowRect, i, _visibleRows[i]);
                DrawFixedCells(rowRect, _visibleRows[i]);
            }

            GUI.EndClip();

            var rightBody = new Rect(rightX, bodyY, rightWidth, bodyHeight);
            _scroll = GUI.BeginScrollView(rightBody, _scroll, new Rect(0f, 0f, contentWidth, contentHeight));
            var rowWidth = Mathf.Max(contentWidth, rightWidth);
            for (var i = first; i <= last; i++)
            {
                var rowRect = new Rect(0f, i * RowHeight, rowWidth, RowHeight);
                var row = _visibleRows[i];
                DrawRowBackground(rowRect, i, row);
                if (_scrollHeader == null)
                    continue;
                var visibleColumns = _scrollHeader.state.visibleColumns;
                for (var v = 0; v < visibleColumns.Length; v++)
                {
                    var columnIndex = visibleColumns[v];
                    if (columnIndex < 0 || columnIndex >= _scrollColumns.Count)
                        continue;
                    var cellRect = _scrollHeader.GetCellRect(v, rowRect);
                    DrawScrollCell(cellRect, row, _scrollColumns[columnIndex]);
                }
            }

            GUI.EndScrollView();
        }

        private void DrawRowBackground(Rect rowRect, int index, Row row)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if (index % 2 == 1)
                EditorGUI.DrawRect(rowRect, ZebraColor);
            if (Selection.activeObject == row.Data)
                EditorGUI.DrawRect(rowRect, SelectedRowColor);
        }

        private void DrawFixedCells(Rect rowRect, Row row)
        {
            var visibleColumns = _fixedHeader.state.visibleColumns;
            for (var v = 0; v < visibleColumns.Length; v++)
            {
                var cell = _fixedHeader.GetCellRect(v, rowRect);
                cell.xMin += 4f;
                switch (visibleColumns[v])
                {
                    case FixedColName:
                        if (GUI.Button(cell, row.NameContent, _linkStyle))
                            SelectAndPing(row.Data);
                        break;
                    case FixedColType:
                        GUI.Label(cell, row.TypeContent, _dimLabelStyle);
                        break;
                    case FixedColBase:
                    {
                        var baseConfig = row.Data.EditorBaseConfig;
                        if (baseConfig == null)
                        {
                            GUI.Label(cell, _missingContent, _missingStyle);
                            break;
                        }

                        _tmpContent.text = baseConfig.name;
                        _tmpContent.tooltip = "";
                        if (GUI.Button(cell, _tmpContent, _linkStyle))
                            EditorGUIUtility.PingObject(baseConfig);
                        break;
                    }
                }
            }
        }

        private void DrawScrollCell(Rect cell, Row row, ColumnDef column)
        {
            switch (column.Kind)
            {
                case ColumnKind.Price:
                    DrawPriceCell(cell, row);
                    break;
                case ColumnKind.FloatTag:
                    DrawFloatCell(cell, row, column.Tag);
                    break;
                case ColumnKind.ObjTag:
                    DrawObjCell(cell, row, column.Tag);
                    break;
            }
        }

        private static Rect FieldRect(Rect cell)
        {
            return new Rect(cell.x + CellPadding, cell.y + CellPadding, cell.width - CellPadding * 2f,
                cell.height - CellPadding * 2f);
        }

        private void DrawPriceCell(Rect cell, Row row)
        {
            var data = row.Data;
            var index = FindPriceIndex(data);
            if (index < 0)
            {
                _tmpContent.text = "—";
                _tmpContent.tooltip = cell.Contains(Event.current.mousePosition)
                    ? "沒掛 PriceData（非賣品，或售價走 d_Price config）"
                    : "";
                GUI.Label(cell, _tmpContent, _missingStyle);
                return;
            }

            var price = ((PriceData)data.DataFunctions[index]).BasePrice;
            EditorGUI.BeginChangeCheck();
            var newPrice = EditorGUI.DelayedFloatField(FieldRect(cell), price, _ownStyle);
            if (EditorGUI.EndChangeCheck() && !Mathf.Approximately(newPrice, price))
                SetPrice(data, index, newPrice);
        }

        //PriceData._basePrice 沒有 setter，走 SerializedObject 寫（ApplyModifiedProperties 自帶 Undo + dirty）
        private static void SetPrice(GameData data, int index, float value)
        {
            var serializedObject = new SerializedObject(data);
            var functions = serializedObject.FindProperty(DataFunctionsFieldName);
            if (functions == null || !functions.isArray || index >= functions.arraySize)
            {
                Debug.LogWarning($"{LogTag} 找不到 {data.name}.{DataFunctionsFieldName}[{index}]，售價沒寫進去", data);
                return;
            }

            var basePrice = functions.GetArrayElementAtIndex(index).FindPropertyRelative(BasePriceFieldName);
            if (basePrice == null)
            {
                Debug.LogWarning($"{LogTag} {data.name} 的 PriceData 上找不到 {BasePriceFieldName}，售價沒寫進去", data);
                return;
            }

            basePrice.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFloatCell(Rect cell, Row row, VariableTag tag)
        {
            var evt = Event.current;
            var data = row.Data;
            var hovered = cell.Contains(evt.mousePosition);

            if (data.EditorTryGetOwnConfig(tag, out var own))
            {
                if (evt.type == EventType.ContextClick && hovered)
                {
                    ShowOwnCellMenu(data, tag);
                    evt.Use();
                }

                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.DelayedFloatField(FieldRect(cell), own, _ownStyle);
                if (EditorGUI.EndChangeCheck() && !Mathf.Approximately(newValue, own))
                    data.EditorSetOwnConfig(tag, newValue);
                return;
            }

            var source = data.EditorFindBaseFloatSource(tag, out var inherited);
            if (source != null)
            {
                if (evt.type == EventType.ContextClick && hovered)
                {
                    ShowInheritedCellMenu(data, tag, source, inherited);
                    evt.Use();
                }

                //tooltip 只在滑過時組字串，避免每次 repaint 每格都配一條
                if (hovered)
                {
                    _tmpContent.text = "";
                    _tmpContent.tooltip = "繼承自 " + source.name + "（直接改值＝在本層建 override）";
                    GUI.Label(cell, _tmpContent, GUIStyle.none);
                }

                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.DelayedFloatField(FieldRect(cell), inherited, _inheritedStyle);
                if (EditorGUI.EndChangeCheck())
                    data.EditorSetOwnConfig(tag, newValue);
                return;
            }

            _missingContent.tooltip = hovered ? "沒有 " + tag.name + "（點一下在本層新增，值 0）" : "";
            if (GUI.Button(cell, _missingContent, _missingStyle) && Event.current.button == 0)
                data.EditorSetOwnConfig(tag, 0f);
            _missingContent.tooltip = "";
        }

        private void DrawObjCell(Rect cell, Row row, VariableTag tag)
        {
            var data = row.Data;
            GUIStyle style;
            MonoObj obj;
            GameData source = null;
            if (data.EditorTryGetOwnObjConfig(tag, out obj))
            {
                style = _objOwnStyle;
            }
            else
            {
                source = data.EditorFindBaseObjSource(tag, out obj);
                if (source == null)
                {
                    GUI.Label(cell, _missingContent, _missingStyle);
                    return;
                }

                style = _objInheritedStyle;
            }

            var inner = FieldRect(cell);
            _tmpContent.text = obj != null ? obj.name : "null";
            _tmpContent.tooltip = cell.Contains(Event.current.mousePosition)
                ? source != null ? "繼承自 " + source.name : "本層 override"
                : "";
            if (GUI.Button(inner, _tmpContent, style) && obj != null)
                EditorGUIUtility.PingObject(obj);
        }

        #endregion

        #region 選單

        private void ShowOwnCellMenu(GameData data, VariableTag tag)
        {
            var menu = new GenericMenu();
            var source = data.EditorFindBaseFloatSource(tag, out var inherited);
            if (source != null)
                menu.AddItem(
                    new GUIContent($"還原成繼承值（{inherited:0.###}，來自 {EscapeMenu(source.name)}）"),
                    false,
                    () => RemoveOwn(data, tag));
            else
                menu.AddItem(
                    new GUIContent("移除這個 key（base 沒有，會整個消失）"),
                    false,
                    () => RemoveOwn(data, tag));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Ping " + EscapeMenu(data.name)), false, () => SelectAndPing(data));
            menu.ShowAsContext();
        }

        private void ShowInheritedCellMenu(GameData data, VariableTag tag, GameData source, float inherited)
        {
            var menu = new GenericMenu();
            menu.AddItem(
                new GUIContent($"在本層建 override（帶入 {inherited:0.###}）"),
                false,
                () =>
                {
                    data.EditorSetOwnConfig(tag, inherited);
                    Repaint();
                });
            menu.AddDisabledItem(new GUIContent("還原成繼承值（本來就是繼承）"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Ping 來源 " + EscapeMenu(source.name)), false,
                () => EditorGUIUtility.PingObject(source));
            menu.ShowAsContext();
        }

        private void RemoveOwn(GameData data, VariableTag tag)
        {
            data.EditorRemoveOwnConfig(tag);
            //base 也沒有的話這個 key 會消失，欄位聯集要重算
            _needsRefresh = true;
            Repaint();
        }

        private void ShowTypeMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("全部"), string.IsNullOrEmpty(_typeFilter), () => SetTypeFilter(""));
            menu.AddSeparator("");
            for (var i = 0; i < _typeOptions.Count; i++)
            {
                var typeName = _typeOptions[i];
                menu.AddItem(new GUIContent(typeName), _typeFilter == typeName, () => SetTypeFilter(typeName));
            }

            menu.ShowAsContext();
        }

        private void SetTypeFilter(string typeName)
        {
            _typeFilter = typeName;
            _needsRefresh = true;
            Repaint();
        }

        private void ShowFolderMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("全部"), string.IsNullOrEmpty(_folderFilter), () => SetFolderFilter(""));
            menu.AddSeparator("");
            for (var i = 0; i < _folderOptions.Count; i++)
            {
                var folder = _folderOptions[i];
                menu.AddItem(new GUIContent(EscapeMenu(ShortFolder(folder))), _folderFilter == folder,
                    () => SetFolderFilter(folder));
            }

            menu.ShowAsContext();
        }

        private void SetFolderFilter(string folder)
        {
            _folderFilter = folder;
            _needsRefresh = true;
            Repaint();
        }

        private void ShowColumnMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("全部顯示"), false, () =>
            {
                _hiddenSet.Clear();
                _hiddenColumnKeys.Clear();
                _needsRefresh = true;
                Repaint();
            });
            menu.AddItem(new GUIContent("全部隱藏"), false, HideAllColumns);
            menu.AddSeparator("");

            if (_anyPriceInFilter)
                AddColumnToggle(menu, "PriceData 售價", PriceKey);

            for (var i = 0; i < _floatTagUnion.Count; i++)
            {
                var tag = _floatTagUnion[i];
                AddColumnToggle(menu, EscapeMenu(tag.name), FloatKeyPrefix + GetTagGuid(tag));
            }

            if (_showObjColumns && _objTagUnion.Count > 0)
            {
                menu.AddSeparator("");
                for (var i = 0; i < _objTagUnion.Count; i++)
                {
                    var tag = _objTagUnion[i];
                    AddColumnToggle(menu, "Obj Config/" + EscapeMenu(tag.name), ObjKeyPrefix + GetTagGuid(tag));
                }
            }

            menu.ShowAsContext();
        }

        private void AddColumnToggle(GenericMenu menu, string label, string key)
        {
            var hidden = _hiddenSet.Contains(key);
            menu.AddItem(new GUIContent(label), !hidden, () => SetColumnHidden(key, !hidden));
        }

        private void HideAllColumns()
        {
            if (_anyPriceInFilter)
                SetColumnHidden(PriceKey, true);
            for (var i = 0; i < _floatTagUnion.Count; i++)
                SetColumnHidden(FloatKeyPrefix + GetTagGuid(_floatTagUnion[i]), true);
            for (var i = 0; i < _objTagUnion.Count; i++)
                SetColumnHidden(ObjKeyPrefix + GetTagGuid(_objTagUnion[i]), true);
        }

        #endregion

        #region util

        private static void SelectAndPing(GameData data)
        {
            if (data == null)
                return;
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
        }

        private static string ShortFolder(string folder)
        {
            return folder.StartsWith("Assets/", StringComparison.Ordinal) ? folder.Substring(7) : folder;
        }

        //GenericMenu 把 "/" 當子選單分隔，資料夾 / tag 名稱裡的斜線換成長得很像的 U+2215
        private static string EscapeMenu(string text)
        {
            return text.Replace('/', '∕');
        }

        #endregion
    }
}
