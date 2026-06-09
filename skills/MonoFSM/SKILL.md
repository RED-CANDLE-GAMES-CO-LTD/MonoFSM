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

## 撰寫 C# 腳本

### Action

```csharp
public class MyAction : AbstractStateAction
{
    [Required] [SerializeField] private VarEntity _target;

    protected override void OnActionExecuteImplement()
    {
        // 動作邏輯
    }
}
```

### Condition

```csharp
public class MyCondition : AbstractConditionBehaviour
{
    protected override bool IsValid => /* 條件邏輯 */;
}
```

### Description override

`AbstractDescriptionBehaviour` 預設 `public virtual string Description => GetType().Name;`，State 樹列表上只看到類別名。**自訂 Action / Condition 應該 override `Description`**，組合關鍵欄位成一句話，方便在 Inspector / State 樹一眼看出每個節點在做什麼，不用點進去看欄位。

```csharp
// HasStateTagCondition
public override string Description => $"Has Tag [{(_tag != null ? _tag.name : "?")}]";

// IsStateCondition
public override string Description => $"Is {_targetState?.name}";
```

要點：
- 欄位空時填 `?`（避免 NRE，並提示尚未設定）
- 不要在字串裡加 `[Action]` / `[Condition]` 之類的 tag，父類的 `DescriptionTag` 會自動加上
- `VarFloatWrapper` 等 wrapper 已有 `Description` 屬性可以直接組合進去

可參考：`1_MonoFSM_Core/Runtime/1_Conditions/HasStateTagCondition.cs`、`IsStateCondition.cs`

## Auto Attributes

```csharp
[Auto]                               // GetComponent<T>()
[AutoParent]                         // GetComponentInParent<T>()
[AutoChildren]                       // GetComponentsInChildren<T>()
[AutoChildren(DepthOneOnly = true)]  // 僅直接子物件
```

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

## 命名規範

- `SerializeField` 和 `public field` 以底線開頭：`_myField`
- 百分比/比例欄位使用 **0~1 範圍**（`[Range(0f, 1f)]`），不用 0~100

## 常用組件清單

見 [references/components.md](references/components.md)。

## EffectDealer / EffectReceiver 系統

定義「誰可以對誰造成效果」的互動系統，見 [references/effect-system.md](references/effect-system.md)。

## ValueSource / Variable 系統

`AbstractValueSource<T>` 泛型基類用於每幀計算並提供值（方向、位置、輸入等）。Variable 系統（VarFloat、VarVector3 等）的 `IsValueExist` 用於判斷 runtime 有效值。詳見 [references/value-source.md](references/value-source.md)。

## VarWrapper 系列（可綁 Var 或填常數的欄位）

`VarFloatWrapper` / `VarIntWrapper` 等 `[Serializable]` 包裝類，讓欄位在 Inspector 二選一：綁一個 `Var` 引用，或直接填常數。取值一律用 `.Value`，宣告預設值用 `new(...)`（如 `private VarIntWrapper _index = new(-1)`），namespace 為 `MonoFSM.Variable`。詳見 [references/var-wrapper.md](references/var-wrapper.md)。

## Data-Driven 控制模式

**核心精神**：用 Var 變數作為組件之間的溝通介面，而非直接呼叫 method。

當一個系統（如角色移動）需要被多個 State/Action 控制時：
1. **在 Controller 上開 Var 欄位**（如 `VarVector3 _moveDirection`、`VarFloat _speedMultiplier`、`VarVector3 _externalForce`）
2. **Controller 的 Simulate() 每幀從 Var 讀取**，驅動底層行為
3. **各 State/Action 透過通用的 SetVarAction 寫入 Var**，不需要為每個操作寫專用 Action

**範例**：
- Chase State：Action 計算目標方向 → 寫入 `_moveDirection`
- Hurt State：進入時 SetVarAction 把 `_moveDirection` 設為 `(0,0,0)` → 角色停下
- Slow Debuff：SetVarAction 把 `_speedMultiplier` 設為 `0.5` → 角色減速
- 擊退：SetVarAction 把 `_externalForce` 設為擊退向量 → Controller 自動衰減

**好處**：
- 不需要為每種操作寫封裝 Action（如 StopCharacterAction、SlowAction）
- 通用 Action 即可覆蓋大部分需求
- 新的控制需求只需新增 Var 欄位，不需改 Action 程式碼

**何時該寫專用 Action**：當操作涉及複雜邏輯（如計算追蹤方向、路徑規劃）而非單純設值時。

**參考實作**：`SimpleChController`（MonoFSM-Pro/Runtime/GamePlay/Source/Characters/）

## Callback Cache → Simulate 統一處理模式

Unity 回調（`OnCollisionEnter`、`OnEnable`、`OnDisable`、`OnTriggerEnter` 等）的觸發時機不可控：可能在同一幀多次觸發、可能在 Simulate 執行順序之外發生。**不要在回調中直接執行邏輯或修改狀態**，改為 cache 資料/flag，在 `Simulate()` 統一處理。

**原則**：
1. 回調只做 **cache**（設 flag、存資料），不改核心狀態
2. `Simulate()` 開頭檢查 cache，**統一處理一次後清除**
3. 重複觸發時只保留第一筆（`if (_cached) return;`）

```csharp
// 回調只 cache
private bool _cached;
private SomeData _pendingData;

private void OnXxx(...)  // OnCollisionEnter / OnEnable / OnTriggerEnter 等
{
    if (_cached) return;
    _pendingData = ...;
    _cached = true;
}

public void Simulate(float deltaTime)
{
    if (_cached) { /* 處理 _pendingData */ _cached = false; }
    // ... 正常邏輯 ...
}
```

**參考實作**：
- 碰撞反彈：`SimpleFlyingCharacter`（MonoFSM-Pro/Runtime/GamePlay/Source/Characters/）
- Enable/Disable 事件：`OnEnableInvoker`（MonoFSM/1_MonoFSM_Core/Runtime/Module/）

## Raycast 慣例

**所有 raycast 應透過 `IRaycastProcessor`**（namespace `MonoFSM.PhysicsWrapper`），不要直接呼叫 `Physics.Raycast`。讓專案可以集中替換實作（如紀錄、debug overlay、Fusion lag compensation 等）。

取得方式：
```csharp
private IRaycastProcessor _raycastProcessor;
private IRaycastProcessor RaycastProcessor =>
    _raycastProcessor ??= WorldUpdateSimulator
        .GetWorldUpdateSimulator(gameObject)
        ?.GetCompCache<IRaycastProcessor>();
```

使用時建議保留 `Physics.Raycast` fallback（當 simulator 不存在於 prefab preview 或 edit-time 時仍能運作）：
```csharp
if (RaycastProcessor != null)
    RaycastProcessor.Raycast(origin, dir, out hit, dist, mask, QueryTriggerInteraction.Ignore);
else
    Physics.Raycast(origin, dir, out hit, dist, mask, QueryTriggerInteraction.Ignore);
```

同介面群還有 `ISphereCastProcessor` / `ICapsuleRaycastProcessor` / `IBoxCastProcessor`，需要 SphereCast / BoxCast 時用對應介面。

**參考實作**：`MonoFSM/MonoFSM_Physics/Runtime/Interact/SpatialDetection/Raycast/MyRaycast.cs`
**介面定義**：`MonoFSM/MonoFSM_Physics/Runtime/Interact/SpatialDetection/Raycast/IRaycastProcessor.cs`

## C# 效能模式

撰寫 MonoFSM 相關 C# 程式碼時的 GC 避免技巧，見 [references/csharp-patterns.md](references/csharp-patterns.md)。

## Serialized 欄位型別遷移

需要把已序列化的欄位改成不同型別（如 `VarFloat` 直接參照 → `VarFloatWrapper`）又不想掉 prefab reference 時，見 [references/serialization-migration.md](references/serialization-migration.md)。涵蓋為何直接改型別一定掉 ref、legacy 欄位 + `FormerlySerializedAs` 接舊資料、`LoadPrefabContents` 批次遷移、驗證與清孤兒資料的完整 6 步流程。
