---
name: MonoFSM
description: MonoFSM 有限狀態機框架的使用指南。當需要：(1) 了解 MonoFSM 架構與設計理念 (2) 在 Unity Scene 中新增/修改 State、Transition、Condition、Action (3) 撰寫新的 Action、Condition C# 腳本 (4) 使用 Auto 系列 Attribute 自動引用組件 (5) 理解狀態優先級系統 (6) 設定 VarFloat 計時器 (7) 使用 EffectDealer/EffectReceiver 互動系統 (8) 解析、匯出、或讀懂既有 FSM prefab／scene 物件的結構（用 FsmTextExporter 匯出 markdown 文字）時使用此 skill。
---

# MonoFSM

以 GameObject 層級為核心的有限狀態機框架。

## 核心設計

**GameObject 層級表達式**：狀態、轉換、動作和條件都是場景中的 GameObject。

```
[FSM Root]                           # MonoFSMOwner
├── [VarFolder] VariableFolder       # 變數區（VariableFolder + StateMachineLogic）
│   └── f_varName                    # VarFloat / VarEntity 等
│       └── BoundModifier            # VariableFloatBoundModifier（可選）
├── [SchemaFolder] SchemaFolder
├── [StateFolder] StateFolder        # StateMachineLogic 主體
│   └── [State] StateName            # GeneralState
│       ├── OnStateEnterHandler      # 進入時觸發 → 子層放 Action
│       │   └── [Action] XxxAction   # AbstractStateAction 實作
│       ├── [Timer] TimerName        # VarFloatCountDownTimer（可選）
│       └── [Transition] => Target   # TransitionBehaviour
│           └── [Condition] Name     # AbstractConditionBehaviour 實作
└── Context                          # MonoContext
```

## 場景編輯（Unity MCP）

直接用 MCP 工具在 Scene 中編輯 FSM，見 [references/scene-editing.md](references/scene-editing.md)。

## 程式化讀取 FSM 結構

實作 Editor 工具（匯出、視覺化、批次修改）需要 traverse MonoFSM 階層時，見 [references/fsm-traversal.md](references/fsm-traversal.md)。涵蓋 StateFolder 偵測、變數/狀態/轉換/條件/動作的走訪規則，以及 `AnimatorPlayAction` 不繼承 `AbstractStateAction` 等 gotcha。實作範本：`MonoFSM/1_MonoFSM_Core/Editor/PrefabExporter/FsmTextExporter.cs`。

## Auto Attributes

```csharp
[Auto]                               // GetComponent<T>()
[AutoParent]                         // GetComponentInParent<T>()
[AutoChildren]                       // GetComponentsInChildren<T>()
[AutoChildren(DepthOneOnly = true)]  // 僅直接子物件
```

欄位型別可以是 interface（`[AutoParent] private ICurrentEntityOwner _owner;`），底層走 `GetComponentInParent(Type, true)`，取到的是**最近的一顆** parent，可用來當「多種容器共用同一個 child 元件」的自動接線。

### Editor 下 Auto 欄位還沒解析：用 AutoReferenceFieldEditor

Auto 系列在 **editor 下要等 Inspector 被點開才會解析**，所以 `Description`、`IsValid` 這類「畫 hierarchy 就會被呼叫」的成員裡，Auto 欄位常常還是 null → NRE 或一直噴 error。**不要**自己寫 `GetComponentInParent` 補，也不要用 `if (Application.isPlaying)` 迴避，要當場補解析：

```csharp
[ShowInInspector] [AutoParent] private ICurrentEntityOwner _owner;

private ICurrentEntityOwner Owner
{
    get
    {
        if (_owner == null)
            AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_owner));
        return _owner;
    }
}
```

- `AutoAttributeManager` 在 global namespace，不用 using；方法標了 `[Conditional("UNITY_EDITOR")]`，build 時整個 call site 被移除，內部又自己 `if (Application.isPlaying) return`，**runtime 零成本**
- 走的是同一顆 attribute 的 `Execute`，`LimitedType` / includeSelf 等設定都會被尊重，不會把語意寫死
- 反射結果進 `FieldCache`，比每次 `GetComponentInParent` 便宜
- 即使補了解析仍可能是 null（真的沒接），呼叫端還是要 null guard；**error log 只在 `Application.isPlaying` 時才印**，否則 editor 會刷滿 console
- 既有範例：`AbstractMonoVariable.HasParentVarEntity`、`MonoEntity._fsmLogic`、`MonoBlackboard` 的各 folder、`ValueProvider._parentEntity`、`VarEntityCurrentItem.Owner`

## 其他常用 Attributes

```csharp
[Required]       // 必填欄位（Inspector 警告）
[CompRef]        // 標記為組件引用
[DropDownRef]    // 下拉選擇（需手動在 Inspector 設定，MCP 無法設定此類型）
[SOConfig("子資料夾名")] // ScriptableObject 欄位用，提供 Create 按鈕與路徑選擇器
```

### SOConfig 注意事項

`[SOConfig]` 的 Drawer（`SOConfigAttributeDrawer`）使用 `IList.Add()` 新增資產，因此：
- **集合欄位必須用 `List<T>`，不可用 `T[]`**（原生陣列大小固定，`Add()` 會拋 `NotSupportedException`）
- 範例：`[SerializeField] [SOConfig("StateTags")] private List<StateTag> _stateTags = new();`

## 狀態優先級

狀態有 `Priority` 屬性，高優先級狀態不會被低優先級狀態打斷。

## 狀態進入條件

**優先把「能否進入此 State」的條件放在目標 State 的 `CanEnterState`。**
不要把相同條件分散複製到各個來源 State 的 Transition；這樣多個 State 要轉入同一目標時，只需各自建立轉向該 State 的 Transition，進入資格仍由目標 State 統一維護。

只有條件確實取決於「從哪個來源 State 離開」時，才放在該來源的 Transition。

## 命名規範

- `SerializeField` 和 `public field` 以底線開頭：`_myField`
- 百分比/比例欄位使用 **0~1 範圍**（`[Range(0f, 1f)]`），不用 0~100
- **新寫的 component（繼承鏈上有 `AbstractDescriptionBehaviour`）一律 override `Description`**，把關鍵欄位組成一句話，hierarchy / State 樹才看得懂。細節與陷阱見 [references/writing-actions.md](references/writing-actions.md#description-override每個新-component-都要做)

## 常用組件清單

見 [references/components.md](references/components.md)。

## EffectDealer / EffectReceiver 系統

定義「誰可以對誰造成效果」的互動系統，見 [references/effect-system.md](references/effect-system.md)。

物件（receiver 端）要讀「誰在跟我互動」身上的值時，走 best match 的 `EffectEnterBestMatchNode._hittingEntity`，**不要取本機玩家**；組法與坑見 effect-system.md 的「在物件上取『誰在跟我互動』的 selector entity」。

## ValueSource / Variable 系統

`AbstractValueSource<T>` 泛型基類用於每幀計算並提供值（方向、位置、輸入等）。Variable 系統（VarFloat、VarVector3 等）的 `IsValueExist` 用於判斷 runtime 有效值。詳見 [references/value-source.md](references/value-source.md)。

**需要「目標位置」時，用 `TargetPositionResolver`（namespace `MonoValueProvider`，在 Core），不要在欄位寫死 `Transform`**。它是 `[Serializable]`，統一解析 `VarVector3` / `VarTransform` / `VarEntity` 三種來源（優先序：Vector3 > Transform > Entity，各自 `IsValueExist` 才採用）。常用 API：`GetTargetPosition(fallback)`、`ResolvedTransform`、`HasTarget`、`ActiveSource`、`ClearPositionTarget()`。用法：欄位宣告 `[InlineProperty][HideLabel] public TargetPositionResolver _source = new();`，取值前先判 `HasTarget`。位置：`1_MonoFSM_Core/Runtime/0_Pattern/DataProvider/EntityProvider/ValueSource/TargetPositionResolver.cs`。

## VarWrapper 系列（可綁 Var 或填常數的欄位）

`VarFloatWrapper` / `VarIntWrapper` 等 `[Serializable]` 包裝類，讓欄位在 Inspector 二選一：綁一個 `Var` 引用，或直接填常數。取值一律用 `.Value`，宣告預設值用 `new(...)`（如 `private VarIntWrapper _index = new(-1)`），namespace 為 `MonoFSM.Variable`。詳見 [references/var-wrapper.md](references/var-wrapper.md)。

## C# 效能模式

撰寫 MonoFSM 相關 C# 程式碼時的 GC 避免技巧，見 [references/csharp-patterns.md](references/csharp-patterns.md)。

## Serialized 欄位型別遷移

需要把已序列化的欄位改成不同型別（如 `VarFloat` 直接參照 → `VarFloatWrapper`）又不想掉 prefab reference 時，見 [references/serialization-migration.md](references/serialization-migration.md)。涵蓋為何直接改型別一定掉 ref、legacy 欄位 + `FormerlySerializedAs` 接舊資料、`LoadPrefabContents` 批次遷移、驗證與清孤兒資料的完整 6 步流程。

## References

| 檔案 | 什麼情況要讀它 |
|---|---|
| [references/writing-actions.md](references/writing-actions.md) | 要新寫或修改 Action / Condition 的 C# 腳本時。含 Action / Condition 範本、`Description` override 慣例、Render behaviour 掛載位置決定觸發時機（多人時 client 跑不跑）、同一功能要同時支援 Action 與 Render 的 Writer 拆法 |
| [references/design-patterns.md](references/design-patterns.md) | 設計一個新機制、或既有機制會漏狀態／時序出錯時。含 Data-Driven（用 Var 當溝通介面）、持續性狀態改用拉式 Getter + Switch Simulate、Unity 回調 cache 到 Simulate 統一處理、Raycast 一律走 `IRaycastProcessor` |
| [references/scene-editing.md](references/scene-editing.md) | 要在 Unity Scene / prefab 上實際新增或修改 State、Transition、Condition、Action 節點時 |
| [references/fsm-traversal.md](references/fsm-traversal.md) | 寫 Editor 工具要程式化走訪 FSM 階層（匯出、視覺化、批次修改）時 |
| [references/components.md](references/components.md) | 想知道有哪些現成的 State / Action / Condition / Timer 等組件可以直接用，不用自己寫時 |
| [references/effect-system.md](references/effect-system.md) | 處理 EffectDealer / EffectReceiver 互動（誰能對誰造成效果、偵測、判定）時；也含「物件上要取互動者（selector）entity」的組法 |
| [references/value-source.md](references/value-source.md) | 要做每幀計算並提供值的 `AbstractValueSource<T>`，或需要理解 Variable 的 `IsValueExist` 語意時 |
| [references/var-wrapper.md](references/var-wrapper.md) | 欄位要讓使用者在「綁一個 Var」與「直接填常數」之間二選一（`VarFloatWrapper` 等）時 |
| [references/csharp-patterns.md](references/csharp-patterns.md) | 寫每幀執行的程式碼、需要避免 GC 配置時 |
| [references/serialization-migration.md](references/serialization-migration.md) | 要改已序列化欄位的型別又不想掉 prefab reference 時 |
