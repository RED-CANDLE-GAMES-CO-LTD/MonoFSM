using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Core.Editor.PropertyDrawer;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace MonoFSM.Core.Editor.VarQuickCreate
{
    /// <summary>
    ///     選中一個 Var 節點（VarBool / VarFloat / …）按 Alt+V，跳出帶搜尋的 dropdown 列出所有
    ///     「有欄位可以指向這個 Var 型別」的 Action / Condition / Getter，
    ///     選一個就在該 Var 底下生成新 GameObject、掛上元件、把引用指回這個 Var 並 Rename。
    ///     清單是反射掃出來的，新寫的 Action / Condition 會自動出現，不需要維護白名單。
    ///     要把常用的排到置頂區就在型別上標 [QuickCreate]（或 Condition 用既有的 [ConditionPreset]）。
    ///     選中的是 AbstractEventHandler（OnStateEnter / OnPointerClick 之類）時，改列出所有 AbstractStateAction
    ///     子類別直接建成子物件（會被 handler 的 _eventReceivers 抓到，不需要指欄位）。
    /// </summary>
    public static class VarQuickCreateShortcut
    {
        private const string LogTag = "[VarQuickCreate]";
        private const string TopCategory = "★ 常用";

        //預設 Alt+V，可在 Edit/Shortcuts 裡改鍵
        // [Shortcut(
        //     "MonoFSM/Create Action or Condition for Var",
        //     null,
        //     KeyCode.V,
        //     ShortcutModifiers.Shift
        // )]
        [MenuItem("MonoFSM/Create Action or Condition for Var #_v")]
        private static void Open()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning($"{LogTag} 沒有選中 GameObject");
                return;
            }

            //EventHandler 模式：沒有 Var 可指，單純列出所有 Action 建成子物件
            var handler = go.GetComponent<AbstractEventHandler>();
            if (handler != null)
            {
                var eventChildCandidates = GetEventChildCandidates();
                if (eventChildCandidates.Count == 0)
                {
                    Debug.LogWarning(
                        $"{LogTag} 找不到任何 AbstractStateAction / AbstractConditionBehaviour 子類別",
                        go
                    );
                    return;
                }

                ShowDropdown(
                    handler.transform,
                    $"{handler.GetType().Name}　{handler.name}",
                    null,
                    eventChildCandidates
                );
                return;
            }

            //一個節點上理論上只掛一個 Var，多掛時取第一個
            var variables = go.GetComponents<AbstractMonoVariable>();
            if (variables.Length == 0)
            {
                Debug.LogWarning(
                    $"{LogTag} {go.name} 上沒有 Var 元件（AbstractMonoVariable）或 AbstractEventHandler",
                    go
                );
                return;
            }

            var variable = variables[0];
            if (variables.Length > 1)
                Debug.LogWarning(
                    $"{LogTag} {go.name} 上有 {variables.Length} 個 Var，只處理第一個 {variable.GetType().Name}",
                    variable
                );

            var varType = variable.GetType();
            var candidates = GetCandidates(varType);
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"{LogTag} 找不到任何可以指向 {varType.Name} 的 Action / Condition", go);
                return;
            }

            ShowDropdown(
                variable.transform,
                $"{variable.GetType().Name}　{variable.name}",
                variable,
                candidates
            );
        }

        #region Dropdown UI

        //記住上次選到哪，連續建立同一種時比較快
        private static readonly AdvancedDropdownState _dropdownState = new();

        private static void ShowDropdown(
            Transform parent,
            string header,
            AbstractMonoVariable variable, //可為 null（EventHandler 模式沒有 var）
            List<Candidate> candidates
        )
        {
            var dropdown = new CandidateDropdown(
                _dropdownState,
                parent,
                header,
                variable,
                candidates
            );
            //Shortcut 是在 window 的 event 處理中觸發，通常拿得到 mousePosition
            var mouse = Event.current?.mousePosition ?? new Vector2(200, 200);
            dropdown.Show(new Rect(mouse.x, mouse.y, 320, 0));
        }

        private class CandidateItem : AdvancedDropdownItem
        {
            public readonly Candidate _candidate;

            public CandidateItem(string name, Candidate candidate)
                : base(name)
            {
                _candidate = candidate;
            }
        }

        private class CandidateDropdown : AdvancedDropdown
        {
            private readonly List<Candidate> _candidates;
            private readonly string _header;
            private readonly Transform _parent;
            private readonly AbstractMonoVariable _variable; //可為 null

            public CandidateDropdown(
                AdvancedDropdownState state,
                Transform parent,
                string header,
                AbstractMonoVariable variable,
                List<Candidate> candidates
            )
                : base(state)
            {
                _parent = parent;
                _header = header;
                _variable = variable;
                _candidates = candidates;
                minimumSize = new Vector2(320, 340);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem(_header);

                //置頂區維持扁平（不用再點一層），但標上 Kind 才看得出是 Action 還是 Condition
                var hasTop = false;
                foreach (var c in _candidates)
                {
                    if (!c.IsTop)
                        continue;
                    hasTop = true;
                    root.AddChild(new CandidateItem($"{c.Kind}／{c.ItemLabel}", c));
                }

                if (hasTop)
                    root.AddSeparator();

                //其餘依 Kind 分組
                AdvancedDropdownItem group = null;
                var currentKind = "";
                foreach (var c in _candidates)
                {
                    if (c.IsTop)
                        continue;
                    if (group == null || c.Kind != currentKind)
                    {
                        currentKind = c.Kind;
                        group = new AdvancedDropdownItem(currentKind);
                        root.AddChild(group);
                    }

                    group.AddChild(new CandidateItem(c.ItemLabel, c));
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is CandidateItem candidateItem)
                    Create(_parent, _variable, candidateItem._candidate);
            }
        }

        #endregion

        #region Create

        private static void Create(
            Transform parent,
            AbstractMonoVariable variable,
            Candidate candidate
        )
        {
            var undoName = "Create " + candidate.CompType.Name;

            var go = new GameObject(candidate.CompType.Name);
            StageUtility.PlaceGameObjectInCurrentStage(go);
            Undo.RegisterCreatedObjectUndo(go, undoName);
            Undo.SetTransformParent(go.transform, parent, false, undoName);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var comp = Undo.AddComponent(go, candidate.CompType);
            //EventHandler 模式沒有 Var 也沒有欄位要指，直接跳過回填
            if (variable != null && candidate.FieldPath != null && !AssignVar(comp, candidate, variable))
            {
                Debug.LogWarning(
                    $"{LogTag} {candidate.CompType.Name} 的欄位 {candidate.FieldPathName} 回填失敗，"
                    + "元件已建立但引用要自己指",
                    comp
                );
            }

            candidate.PresetSetup?.Invoke(null, new object[] { comp });

            InvokeRename(comp);
            EditorUtility.SetDirty(comp);
            EditorUtility.SetDirty(go);

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            if (variable != null)
                Debug.Log(
                    $"{LogTag} 在 {parent.name} 底下建立 {candidate.CompType.Name}，"
                    + $"{candidate.FieldPathName} → {variable.name}",
                    comp
                );
            else
                Debug.Log(
                    $"{LogTag} 在 {parent.name} 底下建立 {candidate.CompType.Name}",
                    comp
                );
        }

        /// <summary>沿著 FieldPath 走到最後一層把 Var 塞進去，中間的 wrapper 是 null 就 new 一個</summary>
        private static bool AssignVar(
            Component comp,
            Candidate candidate,
            AbstractMonoVariable variable
        )
        {
            object target = comp;
            var path = candidate.FieldPath;
            for (var i = 0; i < path.Length - 1; i++)
            {
                var holder = path[i].GetValue(target);
                if (holder == null)
                {
                    //wrapper 都是 class（PickField 已排除 struct），new 完塞回去才不會改到副本
                    holder = Activator.CreateInstance(path[i].FieldType);
                    path[i].SetValue(target, holder);
                }

                target = holder;
            }

            if (target == null)
                return false;
            path[^1].SetValue(target, variable);
            return true;
        }

        //Rename 是 protected virtual，往 base 一層層找
        private static void InvokeRename(Component comp)
        {
            const BindingFlags bf =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var t = comp.GetType(); t != null; t = t.BaseType)
            {
                var m = t.GetMethod("Rename", bf, null, Type.EmptyTypes, null);
                if (m == null)
                    continue;
                m.Invoke(comp, null);
                return;
            }
        }

        #endregion

        #region Candidate Collection

        private class Candidate
        {
            public Type CompType;
            public string DisplayName;

            /// <summary>回填路徑，長度 1 = 直接欄位，長度 2 = wrapper 之類的巢狀欄位</summary>
            public FieldInfo[] FieldPath;

            public bool IsNested;
            public bool IsTop; //置頂區（有 QuickCreate / ConditionPreset）
            public string Kind;
            public MethodInfo PresetSetup; //預填 method，可為 null
            public int Priority;
            public int SortKey;

            public string FieldPathName =>
                FieldPath == null ? "" : string.Join(".", FieldPath.Select(f => f.Name));

            //巢狀時把欄位路徑標出來，才看得出值是塞進 wrapper 還是直接欄位
            public string ItemLabel => IsNested ? $"{DisplayName}  ({FieldPathName})" : DisplayName;
        }

        private static readonly Dictionary<Type, List<Candidate>> _cacheByVarType = new();

        //EventHandler 子物件候選跟目標無關，全域只有一份
        private static List<Candidate> _eventChildCandidatesCache;

        [InitializeOnLoadMethod]
        private static void ResetCache()
        {
            _cacheByVarType.Clear();
            _eventChildCandidatesCache = null;
        }

        private static List<Candidate> GetEventChildCandidates()
        {
            if (_eventChildCandidatesCache != null)
                return _eventChildCandidatesCache;
            _eventChildCandidatesCache = CollectEventChildCandidates();
            return _eventChildCandidatesCache;
        }

        /// <summary>
        ///     EventHandler 模式：所有具體的 AbstractStateAction（被 _eventReceivers 抓）與
        ///     AbstractConditionBehaviour（被 _conditionFolder 抓），兩者都是 depth-one 子物件自動接上，
        ///     沒有欄位要回填，所以也不收 method 級的 [QuickCreate] / [ConditionPreset] preset
        ///     （那些是設計給「指定回填哪個 Var 欄位」用的）。class 級 [QuickCreate] 只拿來做常用排序。
        ///     KindOf 會把兩者分到不同組，dropdown 依 Kind 分組時自然分開顯示。
        /// </summary>
        private static List<Candidate> CollectEventChildCandidates()
        {
            var list = new List<Candidate>();
            AddPlainCandidates(list, TypeCache.GetTypesDerivedFrom<AbstractStateAction>());
            AddPlainCandidates(list, TypeCache.GetTypesDerivedFrom<AbstractConditionBehaviour>());

            return list.OrderBy(c => c.SortKey)
                .ThenByDescending(c => c.Priority)
                .ThenBy(c => c.Kind)
                .ThenBy(c => c.DisplayName)
                .ToList();
        }

        /// <summary>不指欄位的候選：只用 class 級 [QuickCreate] 做常用排序</summary>
        private static void AddPlainCandidates(List<Candidate> list, IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                if (t.IsAbstract)
                    continue;

                var classAttr = t.GetCustomAttribute<QuickCreateAttribute>();
                var isTop = classAttr != null;
                list.Add(new Candidate
                {
                    CompType = t,
                    FieldPath = null,
                    DisplayName = string.IsNullOrEmpty(classAttr?.DisplayName)
                        ? t.Name
                        : classAttr.DisplayName,
                    Kind = KindOf(t),
                    IsNested = false,
                    IsTop = isTop,
                    Priority = classAttr?.Priority ?? 0,
                    PresetSetup = null,
                    SortKey = isTop ? 0 : 1,
                });
            }
        }

        private static List<Candidate> GetCandidates(Type varType)
        {
            if (_cacheByVarType.TryGetValue(varType, out var cached))
                return cached;

            var list = Collect(varType);
            _cacheByVarType[varType] = list;
            return list;
        }

        private static List<Candidate> Collect(Type varType)
        {
            var list = new List<Candidate>();

            foreach (var t in TypeCache.GetTypesDerivedFrom<AbstractDescriptionBehaviour>())
            {
                if (t.IsAbstract)
                    continue;
                //Var 自己不算候選（它們也有指向別的 Var 的欄位，但不是用來「對這個 Var 做事」的）
                if (typeof(AbstractMonoVariable).IsAssignableFrom(t))
                    continue;

                var presets = CollectPresets(t);
                var kind = KindOf(t);
                var classAttr = t.GetCustomAttribute<QuickCreateAttribute>();

                //一個 preset 一個選項（已預填值）
                foreach (var p in presets)
                {
                    var field = PickField(t, varType, p.FieldName, out var isExact,
                        out var isNested);
                    if (field == null)
                        continue;
                    list.Add(
                        MakeCandidate(t, field, kind, isExact, isNested, p.DisplayName, p.Priority,
                            p.Setup, true)
                    );
                }

                //裸型別也留一個選項（preset 沒涵蓋的組合要自己填欄位）
                var mainField = PickField(
                    t,
                    varType,
                    classAttr?.FieldName,
                    out var exact,
                    out var nested
                );
                if (mainField == null)
                    continue;

                list.Add(
                    MakeCandidate(
                        t,
                        mainField,
                        kind,
                        exact,
                        nested,
                        string.IsNullOrEmpty(classAttr?.DisplayName)
                            ? t.Name
                            : classAttr.DisplayName,
                        classAttr?.Priority ?? 0,
                        null,
                        classAttr != null
                    )
                );
            }

            return list.OrderBy(c => c.SortKey)
                .ThenByDescending(c => c.Priority)
                .ThenBy(c => c.Kind)
                .ThenBy(c => c.DisplayName)
                .ToList();
        }

        private static Candidate MakeCandidate(
            Type compType,
            FieldInfo[] fieldPath,
            string kind,
            bool isExact,
            bool isNested,
            string displayName,
            int priority,
            MethodInfo presetSetup,
            bool isTop
        )
        {
            //置頂 → 精確型別欄位 → 父型別欄位（通用工具）
            var sortKey = isTop ? 0 : isExact ? 1 : 2;
            return new Candidate
            {
                CompType = compType,
                FieldPath = fieldPath,
                DisplayName = string.IsNullOrEmpty(displayName) ? compType.Name : displayName,
                Kind = kind,
                IsNested = isNested,
                IsTop = isTop,
                Priority = priority,
                PresetSetup = presetSetup,
                SortKey = sortKey,
            };
        }

        #endregion

        #region Presets

        private class PresetInfo
        {
            public string DisplayName;
            public string FieldName;
            public int Priority;
            public MethodInfo Setup;
        }

        /// <summary>[QuickCreate] 標在 static method 上的，加上 Condition 既有的 [ConditionPreset]</summary>
        private static List<PresetInfo> CollectPresets(Type t)
        {
            var list = new List<PresetInfo>();

            const BindingFlags bf =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var m in t.GetMethods(bf))
            {
                var ps = m.GetParameters();
                if (ps.Length != 1 || !ps[0].ParameterType.IsAssignableFrom(t))
                    continue;
                foreach (var a in m.GetCustomAttributes<QuickCreateAttribute>())
                    list.Add(
                        new PresetInfo
                        {
                            DisplayName = string.IsNullOrEmpty(a.DisplayName)
                                ? m.Name
                                : a.DisplayName,
                            FieldName = a.FieldName,
                            Priority = a.Priority,
                            Setup = m,
                        }
                    );
            }

            foreach (var e in ConditionPresetRegistry.All)
            {
                if (e.ConditionType != t)
                    continue;
                list.Add(
                    new PresetInfo
                    {
                        DisplayName = e.DisplayName,
                        Priority = e.Priority,
                        Setup = e.Setup,
                    }
                );
            }

            return list;
        }

        #endregion

        #region Field Picking

        private static string KindOf(Type t)
        {
            if (typeof(AbstractConditionBehaviour).IsAssignableFrom(t))
                return "If 條件";
            if (typeof(AbstractStateAction).IsAssignableFrom(t))
                return "Action 動作";
            if (InheritsName(t, "AbstractRenderBehaviour"))
                return "Render";
            return "Getter 其他";
        }

        private static bool InheritsName(Type t, string baseName)
        {
            for (var b = t; b != null; b = b.BaseType)
                if (b.Name == baseName)
                    return true;
            return false;
        }

        private const BindingFlags FieldFlags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        /// <summary>
        ///     挑出這個型別上最適合指向 varType 的 serialized 欄位（含 VarFloatWrapper 這類巢狀一層的）。
        ///     一個型別可能有多個（如 SetVarBoolAction 的 _target 與 _sourceVar），
        ///     用「精確型別 &gt; Required &gt; DropDownRef &gt; 非巢狀 &gt; 宣告順序」挑主要那個。
        /// </summary>
        private static FieldInfo[] PickField(
            Type compType,
            Type varType,
            string explicitFieldName,
            out bool isExact,
            out bool isNested
        )
        {
            isExact = false;
            isNested = false;

            if (!string.IsNullOrEmpty(explicitFieldName))
            {
                var path = ResolveExplicitPath(compType, explicitFieldName, varType);
                if (path == null)
                {
                    Debug.LogWarning(
                        $"{LogTag} {compType.Name} 的 [QuickCreate] FieldName=\"{explicitFieldName}\" 解不到可用欄位"
                    );
                    return null;
                }

                isExact = path[^1].FieldType == varType;
                isNested = path.Length > 1;
                return path;
            }

            FieldInfo[] best = null;
            var bestScore = int.MinValue;

            for (var t = compType; t != null; t = t.BaseType)
                foreach (var f in t.GetFields(FieldFlags))
                {
                    if (!IsSerializedField(f))
                        continue;

                    //直接欄位
                    if (IsVarField(f.FieldType, varType))
                    {
                        var score = ScoreOf(f, f.FieldType == varType, false);
                        if (score <= bestScore)
                            continue;
                        bestScore = score;
                        best = new[] { f };
                        isExact = f.FieldType == varType;
                        isNested = false;
                        continue;
                    }

                    //巢狀一層：VarFloatWrapper / VarFoldOut / TargetPositionResolver 這類 [Serializable] holder
                    if (!IsNestableHolder(f.FieldType))
                        continue;
                    foreach (var inner in EnumerateFields(f.FieldType))
                    {
                        if (!IsSerializedField(inner) || !IsVarField(inner.FieldType, varType))
                            continue;
                        var score = ScoreOf(inner, inner.FieldType == varType, true);
                        if (score <= bestScore)
                            continue;
                        bestScore = score;
                        best = new[] { f, inner };
                        isExact = inner.FieldType == varType;
                        isNested = true;
                    }
                }

            return best;
        }

        private static int ScoreOf(FieldInfo f, bool exact, bool nested)
        {
            var score = 0;
            if (exact)
                score += 16;
            if (HasAttributeNamed(f, "RequiredAttribute"))
                score += 8;
            if (HasAttributeNamed(f, "DropDownRefAttribute"))
                score += 4;
            if (!nested)
                score += 2;
            return score;
        }

        private static FieldInfo[] ResolveExplicitPath(Type compType, string fieldName,
            Type varType)
        {
            var parts = fieldName.Split('.');
            if (parts.Length > 2)
                return null;

            var first = FindField(compType, parts[0]);
            if (first == null)
                return null;
            if (parts.Length == 1)
                return IsVarField(first.FieldType, varType) ? new[] { first } : null;

            var second = FindField(first.FieldType, parts[1]);
            if (second == null || !IsVarField(second.FieldType, varType))
                return null;
            return new[] { first, second };
        }

        private static FieldInfo FindField(Type t, string name)
        {
            for (var cur = t; cur != null; cur = cur.BaseType)
            {
                var f = cur.GetField(name, FieldFlags);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static IEnumerable<FieldInfo> EnumerateFields(Type t)
        {
            for (var cur = t; cur != null; cur = cur.BaseType)
                foreach (var f in cur.GetFields(FieldFlags))
                    yield return f;
        }

        private static bool IsVarField(Type fieldType, Type varType) =>
            typeof(AbstractMonoVariable).IsAssignableFrom(fieldType)
            //欄位型別要能裝得下這個 Var
            && fieldType.IsAssignableFrom(varType);

        /// <summary>可以往裡面找一層 Var 欄位的 [Serializable] class（wrapper / resolver 之類）</summary>
        private static bool IsNestableHolder(Type t)
        {
            if (t.IsValueType || t.IsAbstract || t.IsArray || t.IsPrimitive)
                return false;
            if (t == typeof(string))
                return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                return false;
            if (t.GetCustomAttribute<SerializableAttribute>() == null)
                return false;
            //要能 new 出來才回填得進去
            return t.GetConstructor(Type.EmptyTypes) != null;
        }

        private static bool IsSerializedField(FieldInfo f)
        {
            if (f.IsStatic)
                return false;
            if (f.GetCustomAttribute<NonSerializedAttribute>() != null)
                return false;
            if (f.IsPublic)
                return true;
            return f.GetCustomAttribute<SerializeField>() != null;
        }

        private static bool HasAttributeNamed(FieldInfo f, string attributeTypeName)
        {
            foreach (var a in f.GetCustomAttributes(false))
                if (a.GetType().Name == attributeTypeName)
                    return true;
            return false;
        }

        #endregion
    }
}
