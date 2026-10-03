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

## Transition 評估順序（AbstractStateBehaviour.IMonoState.OnFixedUpdate）

1. 目前 state 底下的 transition 照 **sibling 順序**跑，第一條成立就 `TryActivateState` 並 return。
2. 自己的都沒過，才問 `StateFolder.AllAnyStates` 的 transition；AnyState 指向目前 state 的那條會跳過。
3. 一條 transition 要過：condition 全部 AND（`IsAllValid`）+ source（自己或 AnyState）的 CanExit + target 的 CanEnter；
   接著 `TryActivateState` 再問一次**目前 state** 的 CanExit（含 priority）+ target CanEnter，target 也不能是目前 state。
4. 條件過了但 TryActivateState 被擋，**不會記任何東西**：看 `FsmTrace.CaptureSnapshot` 的 `canExit` / `canEnter` 欄。
5. 因為 2、3 兩道擋，AnyState 目標 state 的 CanEnter **不用自己加「!Is 自己」**。要擋的是「終點 state → 又回到這裡」，例如 Destroying 的 CanEnter 加 `!Is [State] Destroyed`。

### 網路同步會繞過 transition 直接改 state

- `StateMachine.Network.cs` 的 `ReadNetworkData`（IBeforeAllTicks）、`RestoreState`（resim / restore）、proxy 的 `Interpolate()`，
  都會**直接覆寫 `_activeStateId` / `_stateChangeTick`**，不走 transition、CanEnter，也不會跑 OnExit。
- `StateMachine.Render()` 只要看到 state id **或** changeTick 變了，就重跑 `OnEnterStateRender`。所以 client 上看到 enter 的視覺重播，
  不一定是 sim 重進，可能只是 changeTick 被同步蓋過去。
- 懷疑是這條路時，先開 FsmTrace 看 `Previous` 和 `IsResim` / `ProxyRender`。`IsResimFalse` 而且 Previous 是終點 state，就是 authority sim 自己轉的，
  不是網路蓋的，回去查 CanEnter 的 condition。

### 陷阱：module pack 的 state 靠 `_bindingRoot` 找 Owner

- 靠 `StateFolder._bindingRoot` 併進宿主 FSM 的 module pack（例如 Hittable_Destroyable ModulePack），parent 鏈上沒有 MonoFSMOwner。
  `AbstractStateBehaviour.Owner` 找不到的時候，會改抓 binding root 上的 owner（2026-10-01 補的）。
- 補之前 Owner 是 null，`IsStateCondition` 永遠 false，勾 Inverted 就變成永遠 true。實際發生過：`!Is [State] Destroyed` 沒擋到，
  造成 AnyState → Destroying → Destroyed 一直循環。現在 Owner 找不到的 IsStateCondition 會在 hierarchy 標紅。

## ModulePack：併進宿主 entity 的模組

- `MonoModulePack` 放在宿主 entity **直屬** `Modules/`（`MonoModuleFolder`，DepthOneOnly），宿主 `EnterSceneAwake` 的 `BindModulePackFolders`
  把 pack 的 Var / State / EffectDetectable / Schema folder 依型別當 external source 併進宿主。要獨立 state 不跟宿主互斥的模組，root 改掛 `MonoEntity`。
- 宿主自己的四顆 folder 掃描停在 `IEntityScopeBoundary`（`MonoEntity` / `MonoModulePack` / `ModulePackGeometry`），不會把 pack 或 nested entity 的 folder
  當成自己的；宿主完全沒有某種 folder 時才退回舊的深度優先（`AutoChildrenAttribute.FallbackUnboundedIfMissing`）。
- **幾何掛點**：pack 的 `_geometryRoot`（掛 `ModulePackGeometry` 的 `[Geometry]`）runtime 會被 reparent 到宿主 `MonoEntity.ModuleGeometryAnchor`
  （預設 `ViewRoot.Root`；`_moduleGeometryAnchor` 可指定），pose 是宿主存檔時記的相對掛點 pose。**prefab 上看到的位置是搬之前的。**
- **`_receiverScope`**：`HostColliders`（預設）打到宿主任何 collider 都觸發 pack 的 receiver；`OwnGeometryOnly` 只有 `[Geometry]` 底下的 collider 打得到，
  但用 entity 查 receiver 的路（按 E 的 `PlayerInteractState`、`IsEffectDealerOrReceiverCondition`）看不到 pack 的 receiver。
- `EffectDetectable.Get` 跨 external dict 撞 effectType 時 inactive 的 receiver 讓給 active 的。
- 專案端的實例與坑（nested entity 宿主要指掛點、KeepColliderOnMount）看 alishan-code-map `module-assembly.md`。

## 除錯：FsmTrace（state 切換 / effect 命中 / Var 改值的時間軸）

- 開關：Unity 主 toolbar 的 `Trace: On/Off <筆數>`（旁邊 `Trace ▾` 有 Clear / Dump），或 `up menu "Tools/MonoFSM/FSM Trace/Enabled"`。
  Dump 寫到 `Library/FsmTrace/<name>.txt`，用 `up fsm-trace [--entity KW] [--last N]` 讀。
- 不用玩家走過去互動：`up hit <receiver 節點>`（Play Mode，自動用本機玩家同 effectType 的 dealer）→ Dump → `up fsm-trace`。
  完整的自動測試步驟、trace 判讀表和陷阱，看 uprefab skill 的 `references/probe.md`「互動自動測試」。
- kind：`Transition`（`via` 是哪條 transition；`(direct TryActivateState)` = 不是 transition 觸發）/ `Forced` / `Default` /
  `ProxyRender` / `HitEnter` / `HitExit` / `HitBlocked <原因> failCond=#i` / `VarChange <var>: 舊 -> 新 by <writer>` /
  `EventSkipped <原因> <handler>`（EventHandler 被叫到但 action 沒跑：Culling / NotSimulating / ConditionInvalid / NotStateAuthority；
  gameObject inactive 不記、靜態讀 prefab 看 `~`；同原因連續擋只記第一次）。「有 HitEnter 卻沒 VarChange」先找緊接在後的 EventSkipped。
- `VarChange` 預設只記掛 `NetworkedVarTag` 的 Var，而且 Vector3 這類沒欄位存值的型別不記；非 networked 的、或要看 Vector3 的，
  程式裡呼叫 `FsmTrace.WatchVar(var)`（反過來太吵用 `UnwatchVar`）。
- 「為什麼沒轉出去」不會每 tick 記，要拍快照：`FsmTrace.CaptureSnapshot(entity)`（或 `Dump(name, entity)` 會附在檔尾）。
- 逐顆看 condition 結果的標準寫法：`ConditionHelper.FirstFailedIndex(conditions)`（第一顆 false 的 index，-1 = 全過）/
  `EvaluateAt(conditions, i)`，跟 `IsAllValid` 同一套跳過規則。不要自己再寫一份迴圈。
- 從 tick 外面（Editor 工具、scenario runner）要改 gameplay 狀態，排進 `TickActionQueue.Enqueue(ITickAction)`，
  下一個非 resim tick 在 `WorldUpdateSimulator.Simulate` 裡執行；`ExecuteInTick` 回 false 就延到下一個 tick。直接在 Editor update 裡改，
  Fusion 下 networked Var 會被 resim 蓋掉。

## 命名規範

- `SerializeField` 和 `public field` 以底線開頭：`_myField`
- 百分比/比例欄位使用 **0~1 範圍**（`[Range(0f, 1f)]`），不用 0~100
- **新寫的 component（繼承鏈上有 `AbstractDescriptionBehaviour`）一律 override `Description`**，把關鍵欄位組成一句話，hierarchy / State 樹才看得懂。細節與陷阱見 [references/writing-actions.md](references/writing-actions.md#description-override每個新-component-都要做)

## 常用組件清單

見 [references/components.md](references/components.md)。

## EffectDealer / EffectReceiver 系統

定義「誰可以對誰造成效果」的互動系統，見 [references/effect-system.md](references/effect-system.md)。

物件（receiver 端）要讀「誰在跟我互動」身上的值時，走 best match 的 `EffectEnterBestMatchNode._hittingEntity`，**不要取本機玩家**；組法與坑見 effect-system.md 的「在物件上取『誰在跟我互動』的 selector entity」。

**新增 detector 時放現成的 `MonoFSM/0_MonoFSM_Example_Module/[Detector] Trigger.prefab` (guid:cfc3ca4b9e2e5480a8563ebe7e8036b6)，不要手刻**——手刻容易漏掉 kinematic Rigidbody，static-static 的 trigger 完全不觸發且沒有錯誤訊息。細節見 effect-system.md 的「新增 EffectDetector」。

## ValueSource / Variable 系統

`AbstractValueSource<T>` 泛型基類用於每幀計算並提供值（方向、位置、輸入等）。Variable 系統（VarFloat、VarVector3 等）的 `IsValueExist` 用於判斷 runtime 有效值。詳見 [references/value-source.md](references/value-source.md)。

**陷阱：Var 的 LastValue 只在「有被登記 pending」時才 commit**（2026-10-03 起，VariableFolder 不再每 tick 全掃）。`FlagField` 的值變更入口（`SetCurrentValue` / modifier 增減 / `ResetToDefault` / `ClearValue`）會呼叫 `NotifyCommitPending`，下一次 AfterSimulate 才把 `_lastValue` 更新成目前值。**新增任何直接寫 Field 值的路徑，一定要呼叫 `NotifyCommitPending`**，漏了的話 `IsJustBecameTrue`、`VarFloat.IsDirty`、LastValue 比較類 Condition 會卡在舊值。真的會在 setter 外變值的型別，override `IsCommitPolledEveryTick => true` 走每 tick 輪詢。Getter / proxy var 從來不 commit，它們的 LastValue 沒有意義。設計理由見 `1_MonoFSM_Core/Runtime/2_Variable/Progress.md`。

**需要「目標位置」時，用 `TargetPositionResolver`（namespace `MonoValueProvider`，在 Core），不要在欄位寫死 `Transform`**。它是 `[Serializable]`，統一解析 `VarVector3` / `VarTransform` / `VarEntity` 三種來源（優先序：Vector3 > Transform > Entity，各自 `IsValueExist` 才採用）。常用 API：`GetTargetPosition(fallback)`、`ResolvedTransform`、`HasTarget`、`ActiveSource`、`ClearPositionTarget()`。用法：欄位宣告 `[InlineProperty][HideLabel] public TargetPositionResolver _source = new();`，取值前先判 `HasTarget`。位置：`1_MonoFSM_Core/Runtime/0_Pattern/DataProvider/EntityProvider/ValueSource/TargetPositionResolver.cs`。

## VarWrapper 系列（可綁 Var 或填常數的欄位）

`VarFloatWrapper` / `VarIntWrapper` 等 `[Serializable]` 包裝類，讓欄位在 Inspector 二選一：綁一個 `Var` 引用，或直接填常數。取值一律用 `.Value`，宣告預設值用 `new(...)`（如 `private VarIntWrapper _index = new(-1)`），namespace 為 `MonoFSM.Variable`。**數值參數欄位一律用 Wrapper，不要並排寫 `float _x` + `VarFloat _xVar`**。詳見 [references/var-wrapper.md](references/var-wrapper.md)。

## C# 效能模式

撰寫 MonoFSM 相關 C# 程式碼時的 GC 避免技巧，見 [references/csharp-patterns.md](references/csharp-patterns.md)。

要找「哪個 Action / Condition / State / Simulate 慢或吃 GC」：Editor / Development build 下 FSM 分派自帶 ProfilerMarker，名稱是 `FSM.Action/<型別>`、`FSM.Condition/<型別>`、`FSM.State/<擁有者 MonoEntity 名>/<State 節點名>`、`FSM.StateRender/<擁有者>/<State 節點名>`、`BeforeSimulate|Simulate|AfterSimulate/<實作型別>`，進 Play Mode 錄完用 `uprofile top --sort self` / `uprofile gc` 直接看（Edit Mode 錄不到 FSM）。新寫的分派迴圈要加 marker 就走 `FsmProfilerMarkers`（per-type 快取，不要每次 `new ProfilerMarker` 或讀 `name`）。

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
| [references/effect-system.md](references/effect-system.md) | 處理 EffectDealer / EffectReceiver 互動（誰能對誰造成效果、偵測、判定）時；也含「物件上要取互動者（selector）entity」的組法、新增 EffectDetector 該放哪顆 nested prefab 與 kinematic Rigidbody 的坑 |
| [references/value-source.md](references/value-source.md) | 要做每幀計算並提供值的 `AbstractValueSource<T>`，或需要理解 Variable 的 `IsValueExist` / getter 型 `IsNull` 語意、runtime 寫入的 Var 要勾 `_isRuntimeOnly`、或 Var 底下要掛多顆 condition（**只看第一顆 active 的，不是 OR**，要 AND 得包 `CompositeCondition`）時 |
| [references/var-wrapper.md](references/var-wrapper.md) | 欄位要讓使用者在「綁一個 Var」與「直接填常數」之間二選一（`VarFloatWrapper` 等）時 |
| [references/csharp-patterns.md](references/csharp-patterns.md) | 寫每幀執行的程式碼、需要避免 GC 配置時；也含序列化 array 欄位不會是 null 導致 `??=` lazy init 失效的坑 |
| [references/serialization-migration.md](references/serialization-migration.md) | 要改已序列化欄位的型別又不想掉 prefab reference 時 |
