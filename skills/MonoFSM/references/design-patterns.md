# MonoFSM 設計模式

- [Data-Driven 控制模式](#data-driven-控制模式)
- [持續性狀態：拉式 Getter + Switch Simulate，不要用 Enter/Exit 推](#持續性狀態拉式-getter--switch-simulate不要用-enterexit-推)
- [Callback Cache → Simulate 統一處理模式](#callback-cache--simulate-統一處理模式)
- [Raycast 慣例](#raycast-慣例)

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

## 持續性狀態：拉式 Getter + Switch Simulate，不要用 Enter/Exit 推

「**只要 A 成立就維持 B**」這種持續性狀態，不要在 `EffectEnterNode` 設值、`EffectExitNode` 復原。
Enter/Exit 是**邊緣觸發**，只在偵測表變化的那一幀跑一次，任何一邊漏掉狀態就永久卡住：

- Exit 沒觸發 —— detectable 被 despawn / disable / culling 關掉、reset 清空偵測表
- Enter 沒觸發 —— receiver 註冊鏈還沒完成那一幀（見 `EffectDetector` 的 stay 重試）
- 兩個 detector 範圍重疊時，A 的 exit 蓋掉 B 的 enter

改成拉式：**狀態用 Getter VarBool 表達，每幀由 `SwitchCaseActionSimulator` 對齊**。
狀態是每幀重算出來的，沒有「漏一次就錯到底」的問題。

```
[VarFolder] VariableFolder
  [Getter] Dealer $xxx hit any?   <VarBool>            ← 真相來源，每幀重算
    [If] Dealer $xxx hit any?     <IsDealerHitAnyReceiverCondition _dealer=…>

Context/Animator/LogicRoot
  [Switch Simulate] Switch (FirstMatch)  <SwitchCaseActionSimulator _mode=FirstMatch>
    [Case] SwitchCase                    ← 同一個 case 內多個 [If] = AND
      [If] 開關 == True
      [If] Dealer $xxx hit any? == True
      [Action] [Var] d_IsToggleOn = True
    [Case] SwitchCase                    ← FirstMatch，走到這裡代表上面不成立
      [If] 開關 == True
      [Action] [Var] d_IsToggleOn = False
```

**選 condition**：dealer 端問「我現在有沒有壓到任何 receiver」是
`IsDealerHitAnyReceiverCondition`（`_dealer` 指到 `GeneralEffectDealer`）。
`IsBestMatchedReceiverCondition` 是 **receiver 端**問「我是不是被選中的最佳互動目標」，
用在互動提示 / highlight，兩者不要混用。

**Enter/Exit 仍然該用的場合**：一次性的**事件**（撞擊瞬間扣血、噴特效、震動、播音效）。
判斷準則 —— 「錯過一次會怎樣」：錯過一次特效沒差 → 用 Enter；錯過一次狀態就卡住 → 用拉式。

同一個原則的其他長相：`SetHighlightAction` 從「兩個 receiver 各推 enter/exit」改成
`[Getter] Is BestMatched` 單一來源 + RenderLoop 每幀讀。

**成本**：每幀 SetValue 同值只是 field 賦值，不送事件、不產生 GC，不需要自己加 change check。

**實例**：`鑽頭.prefab` 的自動啟動（前方預判 detector → `[Getter] Dealer $d_NavMeshBlocking
hit any?` → Switch Simulate 開關 `d_IsToggleOn`）、`Train FSM Variant.prefab` 的
Target Speed、`水桶 Water Jug.prefab` 的下雨集水。

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
