using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Editor.PropertyDrawer;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace MonoFSM.Core.Editor.VarQuickCreate
{
    /// <summary>
    ///     選中任何節點按 Alt+V，跳出帶搜尋的 dropdown 列出「這個節點底下可以掛什麼」，選一個就生成子
    ///     GameObject、掛上元件並 Rename。
    ///     主要來源是節點上各 component 用 [AutoChildren] 宣告的欄位 —— 那就是它期待底下掛什麼的白名單，
    ///     所以 Transition / EventHandler 的 _conditions、AbstractGetter 的 _conditionGroup（[AutoNested]
    ///     會再往裡面找一層）、_eventReceivers、_renderActions 全都自動涵蓋，建完不用回填欄位，
    ///     AutoAttributeManager 會把子物件抓進去。新寫的 Action / Condition 會自動出現，不需要維護白名單。
    ///     節點上有 Var（VarBool / VarFloat / …）時額外多三組：
    ///     1. 有欄位可以指向這個 Var 型別的 Action / Condition / Getter（建立時順便把引用指回來）
    ///     2. 「ValueSource 值來源」—— value type 對得上的 provider（FloatLiteralComp 這種常數來源），
    ///     建成子物件被 _valueSources 的 [AutoChildren] 抓走
    ///     3. 「Var 變數」—— 所有 AbstractMonoVariable 子類別，容器類（VariableFolder / VarEntity）建成子物件，
    ///     其他 Var 建成 sibling；VarEntity 還會把 EntityTag 宣告了但沒生出來的 Var 置頂
    ///     要把常用的排到置頂區就在型別上標 [QuickCreate]（或 Condition 用既有的 [ConditionPreset]）。
    /// </summary>
    public static class VarQuickCreateShortcut
    {
        private const string LogTag = "[VarQuickCreate]";
        private const int MaxCandidatesPerSlot = 500;
        private const string ValueSourceKind = "ValueSource 值來源";

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

            //主清單：這個節點上的 component 用 [AutoChildren] 宣告了「底下期待掛什麼」，那就是能建的東西
            var candidates = new List<Candidate>(CollectAutoChildrenCandidates(go));

            //節點上有 Var，再加上「有欄位指向這個 Var」的消費者、Var 的 value source、以及 Var 自己
            var variable = go.GetComponent<AbstractMonoVariable>();
            var folder = go.GetComponent<VariableFolder>();
            var varParent = go.transform;
            if (variable != null)
            {
                candidates.AddRange(GetCandidates(variable.GetType()));
                candidates.AddRange(GetVarCandidates());
                //VarEntity / VariableFolder 這種容器類，新 Var 掛在它底下才會被當成它的 property
                var isVarContainer = variable is VarEntity || folder != null;
                if (!isVarContainer && go.transform.parent != null)
                    varParent = go.transform.parent; //其他 Var → sibling
                //VarEntity 的 schema 缺項置頂（跟 inspector 的「加入 Var」同一份來源），可直接搜尋 tag 名字
                if (variable is VarEntity schemaEntity)
                    candidates.InsertRange(0, CollectSchemaCandidates(schemaEntity));
            }
            else if (folder != null)
            {
                candidates.AddRange(GetVarCandidates());
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning(
                    $"{LogTag} {go.name} 上沒有任何 [AutoChildren] 欄位、Var 或 VariableFolder，沒有候選可列",
                    go
                );
                return;
            }

            var header = variable != null
                ? $"{variable.GetType().Name}　{variable.name}"
                : go.name;
            ShowDropdown(go.transform, header, variable, candidates, varParent);
        }

        #region Dropdown UI

        //記住上次選到哪，連續建立同一種時比較快
        private static readonly AdvancedDropdownState _dropdownState = new();

        private static void ShowDropdown(
            Transform parent,
            string header,
            AbstractMonoVariable variable, //可為 null（EventHandler 模式沒有 var）
            List<Candidate> candidates,
            Transform varParent //建 Var 元件時用的 parent（可能是 sibling 的 parent）
        )
        {
            var dropdown = new CandidateDropdown(
                _dropdownState,
                parent,
                header,
                variable,
                candidates,
                varParent
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
            private readonly Transform _varParent;

            public CandidateDropdown(
                AdvancedDropdownState state,
                Transform parent,
                string header,
                AbstractMonoVariable variable,
                List<Candidate> candidates,
                Transform varParent
            )
                : base(state)
            {
                _parent = parent;
                _header = header;
                _variable = variable;
                _candidates = candidates;
                _varParent = varParent;
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
                    root.AddChild(new CandidateItem($"{c.Kind}／{c.SearchLabel}", c));
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

                    group.AddChild(new CandidateItem(c.SearchLabel, c));
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is not CandidateItem candidateItem)
                    return;
                var c = candidateItem._candidate;
                //Schema 缺項走 VarEntity 自己的建立流程（會設 _varTag 與 _parentVarEntity）
                if (c.SchemaTag != null && _variable is VarEntity entity)
                    CreateSchemaVar(entity, c);
                //Var 元件不需要回填欄位，parent 也另外算
                else if (c.IsVarComponent)
                    Create(_varParent, null, c);
                else
                    Create(_parent, _variable, c);
            }
        }

        #endregion

        #region Create

        private static void CreateSchemaVar(VarEntity entity, Candidate candidate)
        {
            Undo.RegisterCompleteObjectUndo(entity.gameObject, "Add Schema Var");
            var variable = entity.AddVarOfSchemaTag(candidate.SchemaTag);
            if (variable == null)
            {
                Debug.LogWarning($"{LogTag} {candidate.SchemaTag.name} 生成失敗", entity);
                return;
            }

            Undo.RegisterCreatedObjectUndo(variable.gameObject, "Add Schema Var");
            Debug.Log(
                $"{LogTag} 在 {entity.name} 底下建立 {candidate.CompType.Name}（tag {candidate.SchemaTag.name}）",
                variable
            );
            SelectDelayed(variable.gameObject);
        }

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

            SelectDelayed(go);
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

        /// <summary>dropdown 關閉那一帧會把 selection 還原回原本的節點，延一帧設才選得到新物件</summary>
        private static void SelectDelayed(GameObject go)
        {
            Selection.activeGameObject = go;
            EditorApplication.delayCall += () =>
            {
                if (go == null)
                    return;
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            };
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
            public bool IsVarComponent; //本身是 AbstractMonoVariable，建 child / sibling 不指欄位
            public VariableTag SchemaTag; //VarEntity 的 schema 缺項，建立時要順便設 _varTag
            public string Kind;
            public MethodInfo PresetSetup; //預填 method，可為 null
            public int Priority;
            public int SortKey;

            public string FieldPathName =>
                FieldPath == null ? "" : string.Join(".", FieldPath.Select(f => f.Name));

            //巢狀時把欄位路徑標出來，才看得出值是塞進 wrapper 還是直接欄位
            public string ItemLabel => IsNested ? $"{DisplayName}  ({FieldPathName})" : DisplayName;

            //AdvancedDropdown 的搜尋是比對 item 的 name，[QuickCreate] 把 DisplayName 換成中文後
            //型別名就搜不到了（打 FloatLiteral 撈不到「常數 Float」），所以補在尾巴當搜尋用的別名
            public string SearchLabel =>
                DisplayName == CompType.Name ? ItemLabel : $"{ItemLabel}  {CompType.Name}";
        }

        private static readonly Dictionary<Type, List<Candidate>> _cacheByVarType = new();

        //Var 候選也跟目標無關
        private static List<Candidate> _varCandidatesCache;

        [InitializeOnLoadMethod]
        private static void ResetCache()
        {
            _cacheByVarType.Clear();
            _candidatesByTargetType.Clear();
            _varCandidatesCache = null;
        }

        //同一個 TargetType 的候選清單只算一次
        private static readonly Dictionary<Type, List<Candidate>> _candidatesByTargetType = new();

        /// <summary>[AutoChildren] 宣告的一個「子物件插槽」</summary>
        private class ChildSlot
        {
            public string FieldPathName; //"_eventReceivers" / "_conditionFolder._conditions"
            public Type TargetType; //LimitedType ?? 欄位（元素）型別

            public string Kind => $"{TargetType.Name}（{FieldPathName}）";
        }

        /// <summary>
        ///     這個節點上所有 component 的 [AutoChildren] 欄位，就是「在這個節點底下建子物件會被誰抓走」的宣告，
        ///     所以拿它們的目標型別當候選來源，建完不用回填欄位（AutoAttributeManager 會抓）。
        ///     [AutoNested] 的欄位往裡面遞迴一層層找（ConditionGroup._conditions 這種）。
        ///     一個 TargetType 只留一組（多個 component 宣告同一種時候選內容一樣）。
        /// </summary>
        private static List<Candidate> CollectAutoChildrenCandidates(GameObject go)
        {
            var slots = new Dictionary<Type, ChildSlot>();
            //有 Var 的話 IValueProvider 那組交給 CollectValueSourceCandidates 依 value type 收窄，
            //不然 _valueSources 會把所有 provider（含每個 Condition）都倒出來
            var narrowedByVar = go.GetComponent<AbstractMonoVariable>() != null;
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) //missing script
                    continue;
                ScanChildSlots(comp.GetType(), "", 0, slots, narrowedByVar);
            }

            var list = new List<Candidate>();
            foreach (var slot in slots.Values)
                list.AddRange(GetCandidatesOfTargetType(slot));
            return list;
        }

        private static void ScanChildSlots(
            Type type,
            string prefix,
            int depth,
            Dictionary<Type, ChildSlot> slots,
            bool narrowedByVar
        )
        {
            if (type == null || depth > 3) //跟 AutoNested 的 maxDepth 一致
                return;

            foreach (var f in EnumerateFields(type))
            {
                var autoChildren = f.GetCustomAttribute<AutoChildrenAttribute>();
                if (autoChildren != null)
                {
                    var target = ResolveSlotTargetType(f, autoChildren);
                    if (target == null || slots.ContainsKey(target))
                        continue;
                    //IValueProvider 太寬（所有 Var / Condition 都是），有 Var 就走收窄過的那組
                    if (narrowedByVar && target == typeof(IValueProvider))
                        continue;
                    slots.Add(target, new ChildSlot
                    {
                        FieldPathName = prefix + f.Name,
                        TargetType = target,
                    });
                    continue;
                }

                if (f.GetCustomAttribute<AutoNestedAttribute>() != null)
                    ScanChildSlots(f.FieldType, prefix + f.Name + ".", depth + 1, slots,
                        narrowedByVar);
            }
        }

        /// <summary>陣列 / List 取元素型別，[AutoChildren(LimitedType = ...)] 優先（那才是真正要撈的型別）</summary>
        private static Type ResolveSlotTargetType(FieldInfo f, AutoChildrenAttribute attr)
        {
            if (attr.LimitedType != null)
                return attr.LimitedType;

            var t = f.FieldType;
            if (t.IsArray)
                t = t.GetElementType();
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                t = t.GetGenericArguments()[0];

            if (t == null)
                return null;
            //要能掛在 GameObject 上才建得出來（interface 型別的實作者稍後再過濾）
            if (!t.IsInterface && !typeof(Component).IsAssignableFrom(t))
                return null;
            return t;
        }

        private static List<Candidate> GetCandidatesOfTargetType(ChildSlot slot)
        {
            if (_candidatesByTargetType.TryGetValue(slot.TargetType, out var cached))
                return cached;

            var types = new List<Type>();
            //欄位型別本身就是具體 component 時（[AutoChildren] private OnStateEnterHandler _onStateEnter
            //這種單一欄位），它就是要建的東西 —— GetTypesDerivedFrom 不含自己，不補這行整組會漏掉
            if (!slot.TargetType.IsAbstract
                && !slot.TargetType.IsInterface
                && typeof(MonoBehaviour).IsAssignableFrom(slot.TargetType))
                types.Add(slot.TargetType);
            foreach (var t in TypeCache.GetTypesDerivedFrom(slot.TargetType))
            {
                if (t.IsAbstract)
                    continue;
                //interface 的實作者可能不是 Component，那就掛不上去
                if (!typeof(MonoBehaviour).IsAssignableFrom(t))
                    continue;
                types.Add(t);
            }

            var list = new List<Candidate>();
            //型別太泛的 slot（MonoObj[] 這種）會倒出上千個選項把 dropdown 塞爆，選了也不會是想要的
            if (types.Count > MaxCandidatesPerSlot)
            {
                Debug.Log(
                    $"{LogTag} {slot.FieldPathName} 的目標型別 {slot.TargetType.Name} 有 {types.Count} 個子類別，"
                    + $"超過 {MaxCandidatesPerSlot} 不列入清單"
                );
                _candidatesByTargetType[slot.TargetType] = list;
                return list;
            }

            AddPlainCandidates(list, types);
            foreach (var c in list)
                c.Kind = slot.Kind;
            list = list
                .OrderByDescending(c => c.Priority)
                .ThenBy(c => c.DisplayName)
                .ToList();
            _candidatesByTargetType[slot.TargetType] = list;
            return list;
        }

        /// <summary>所有具體的 AbstractMonoVariable 子類別，建成 child / sibling，沒有欄位要回填</summary>
        private static List<Candidate> GetVarCandidates()
        {
            if (_varCandidatesCache != null)
                return _varCandidatesCache;
            var list = new List<Candidate>();
            AddPlainCandidates(list, TypeCache.GetTypesDerivedFrom<AbstractMonoVariable>());
            foreach (var c in list)
                c.IsVarComponent = true;
            _varCandidatesCache = list
                .OrderByDescending(c => c.Priority)
                .ThenBy(c => c.DisplayName)
                .ToList();
            return _varCandidatesCache;
        }

        /// <summary>不指欄位的候選：只用 class 級 [QuickCreate] 做常用排序</summary>
        private static void AddPlainCandidates(List<Candidate> list, IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                if (t.IsAbstract)
                    continue;

                var classAttr = t.GetCustomAttribute<QuickCreateAttribute>();
                //Var 是完整清單，全丟置頂區會塞爆，統一放分組裡
                var isTop = classAttr != null
                            && !typeof(AbstractMonoVariable).IsAssignableFrom(t);
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

        /// <summary>VarEntity 的 EntityTag 宣告了、但還沒生出來的 Var（每次重新掃，跟著場上狀態變）</summary>
        private static List<Candidate> CollectSchemaCandidates(VarEntity entity)
        {
            var list = new List<Candidate>();
            foreach (var tag in entity.GetMissingSchemaTags())
            {
                var varType = tag.VariableMonoType;
                if (varType == null || !typeof(AbstractMonoVariable).IsAssignableFrom(varType))
                    continue;
                list.Add(new Candidate
                {
                    CompType = varType,
                    FieldPath = null,
                    DisplayName = $"{tag.name} <{varType.Name}>",
                    Kind = "Schema 缺的 Var",
                    IsNested = false,
                    IsTop = true, //置頂扁平，搜尋時直接打 tag 名字就找得到
                    Priority = 100,
                    PresetSetup = null,
                    SortKey = 0,
                    IsVarComponent = true,
                    SchemaTag = tag,
                });
            }

            return list;
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

            //不指欄位、靠 [AutoChildren] 被 Var 抓進 _valueSources 的值來源
            list.AddRange(CollectValueSourceCandidates(varType));

            return list.OrderBy(c => c.SortKey)
                .ThenByDescending(c => c.Priority)
                .ThenBy(c => c.Kind)
                .ThenBy(c => c.DisplayName)
                .ToList();
        }

        /// <summary>
        ///     這個 Var 的 value source 候選：AbstractMonoVariable._valueSources 標了
        ///     [AutoChildren(DepthOneOnly, _isSelfInclude)]，所以建成 Var 底下的子物件就會自動接上，
        ///     沒有欄位要回填（跟 EventHandler 模式一樣）。方向跟主清單相反 ——
        ///     主清單是「有欄位指向這個 Var」的消費者，這裡是「被這個 Var 拿去取值」的提供者，
        ///     像 FloatLiteralComp 這種只有 float _literal、沒有任何 Var 欄位的常數來源只會出現在這組。
        ///     只收 value type 對得上的（IValueProvider&lt;out T&gt; 是 covariant，子型別的 provider 也餵得進去）。
        /// </summary>
        private static List<Candidate> CollectValueSourceCandidates(Type varType)
        {
            var list = new List<Candidate>();
            var valueType = ResolveVarValueType(varType);
            if (valueType == null)
                return list;

            var providerType = typeof(IValueProvider<>).MakeGenericType(valueType);
            var matched = new List<Type>();
            //_valueSources 是 IValueProvider[]，不是 AbstractGetter[] —— 掃 AbstractGetter 會整批漏掉
            //Condition（AbstractConditionBehaviour 那條線，實作 IValueProvider<bool>/<float>）
            foreach (var t in TypeCache.GetTypesDerivedFrom<IValueProvider>())
            {
                if (t.IsAbstract)
                    continue;
                //要能 AddComponent 到子物件上才餵得進 [AutoChildren]
                if (!typeof(Component).IsAssignableFrom(t))
                    continue;
                //Var 自己也是 provider，但它有自己一組候選（GetVarCandidates）
                if (typeof(AbstractMonoVariable).IsAssignableFrom(t))
                    continue;
                if (!providerType.IsAssignableFrom(t))
                    continue;
                matched.Add(t);
            }

            AddPlainCandidates(list, matched);
            //KindOf 會把它們全歸成「ValueGetter 取值」，跟主清單那組混在一起看不出差別
            foreach (var c in list)
                c.Kind = ValueSourceKind;
            return list;
        }

        /// <summary>TypedMonoVariable&lt;T&gt; 的 T 就是這個 Var 的值型別（解不到就不列 value source）</summary>
        private static Type ResolveVarValueType(Type varType)
        {
            for (var t = varType; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(TypedMonoVariable<>))
                    return t.GetGenericArguments()[0];
            return null;
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
            if (typeof(AbstractMonoVariable).IsAssignableFrom(t))
                return "Var 變數";
            if (typeof(AbstractConditionBehaviour).IsAssignableFrom(t))
                return "If 條件";
            if (typeof(AbstractStateAction).IsAssignableFrom(t))
                return "Action 動作";
            if (typeof(AbstractGetter).IsAssignableFrom(t))
                return "ValueGetter 取值";

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
