# EffectDealer / EffectReceiver 系統

用於定義「誰可以對誰造成效果」的互動系統。兩者需有相同的 `GeneralEffectType` 才能觸發。

## 類別層級

```
EffectResolver (abstract)
├── GeneralEffectDealer   — 發起方（攻擊者、抓取者等）
└── GeneralEffectReceiver — 接收方（可受影響的物件）
```

## GameObject 結構

```
[Dealer GO]
└── GeneralEffectDealer (_effectType = GrabType)
    └── EffectEnterNode
        └── [Action] MyDealerEnterAction   ← AbstractArgEventHandler<GeneralEffectHitData>

[Receiver GO]
└── GeneralEffectReceiver (_effectType = GrabType)
    └── EffectEnterNode
        └── [Action] MyReceiverEnterAction ← AbstractArgEventHandler<GeneralEffectHitData>
```

## GeneralEffectHitData

兩者相交時產生，包含：
- `GeneralDealer` — 發起方的 GeneralEffectDealer
- `GeneralReceiver` — 接收方的 GeneralEffectReceiver
- `Dir` — 從 Dealer 指向 Receiver 的方向
- `hitPoint` / `hitNormal` / `hitDirection` — 可選的碰撞資訊

取得相關組件：
```csharp
data.GeneralDealer.GetComponentInParent<MyDealerScript>();
data.GeneralReceiver.GetComponentInParent<Rigidbody>();
```

## 撰寫 AbstractArgEventHandler 實作

放在 `EffectEnterNode` 下，收到 hit 事件時觸發：

```csharp
public class GrabbableHandlerAction : AbstractArgEventHandler<GeneralEffectHitData>
{
    [DropDownRef] public GravityGrabber _gravityGrabber;  // 直接序列化引用

    protected override void OnActionExecuteImplement()
    {
        throw new System.NotImplementedException(); // 事件驅動，不走這裡
    }

    protected override void OnArgEventReceived(GeneralEffectHitData data)
    {
        var rb = data.GeneralReceiver.GetComponentInParent<Rigidbody>();
        _gravityGrabber.Grabbed(rb);
    }
}
```

## 主要 API

```csharp
// 從 Code 手動觸發（不走 PhysicsDetection）
receiver.OnEffectHitEnter(hitData);  // 觸發 Receiver 的 EffectEnterNode
receiver.OnEffectHitExit(hitData);

dealer.OnHitEnter(hitData);           // 觸發 Dealer 的 EffectEnterNode
dealer.OnHitExit(hitData);

// 一次性觸發（enter+exit 合一，適合 grab/pickup 等瞬間事件）
receiver.ForceDirectEffectHit(dealer, receiverSourceObj);

// 建立 hit data
var hitData = receiver.GenerateEffectHitData(dealer, null);
```

## BestMatchReceiver

當多個 Receiver 同時與 Dealer 重疊時，用距離或自訂分數選出最佳：
```csharp
var best = dealer.BestMatchReceiver;
```

## 在物件上取「誰在跟我互動」的 selector entity

物件（receiver 端）要根據**互動者身上的值**改變行為或文字提示（例如「你手上沒螺絲起子」）時，
**不要去取本機玩家**（`entity_PlayerBrain` + `d_CurrentPlayerEntity` 那條全域 tag 解析鏈是給
GameplayUI 這種本機 UI 用的，物件 prefab 上用它在多人下語意就錯了）。正解是走 best match 的 hitEntity。

資料流（receiver 端；dealer 端對稱，寫入的是對方 receiver 的 entity）：

```
GeneralEffectReceiver.OnEffectHitBestMatchEnter          Resolver/GeneralEffectReceiver.cs:180-184
  → EffectResolver.BestMatchEnterHandle(data, data.GeneralDealer.BindEntity)
                                                          Resolver/EffectResolver.cs:129-133
      → _bestEnterNode._hittingEntity.SetValue(pairEntity)
```

（`MonoFSM/1_MonoFSM_Core/Runtime/EffectHit/` 底下。非 best match 的一般命中走
`OnEffectHitEnter` → `_enterNode._hittingEntity`，`GeneralEffectReceiver.cs:108-112`。）

節點結構範本：

```
[Receiver] Distance Select                <GeneralEffectReceiver>
  [Event] EffectEnterBestMatchNode        <EffectEnterBestMatchNode _clearHittingEntityOnExit=?>
    [Var] <effectType> bestMatch hitEntity  <VarEntity _isRuntimeOnly=true>   ← 名字由框架 Rename 自動產生
      [Var] ….d_HandToolKind                <VarInt _varTag=d_HandToolKind>   ← 巢狀取互動者身上的 var
```

底下那顆巢狀 Var 就能被 `VarIntCompareCondition._varInt._var` / value source 直接引用
（**不需要放在 VariableFolder 裡**，`[Var] Interact hitEntity.d_HandToolKind` 這種寫法就是這樣長的）。

要點與踩過的坑：

- `EffectResolver._bestEnterNode` 是 `[CompRef][AutoChildren(DepthOneOnly = true)]`
  （`EffectResolver.cs:121`）→ **node 必須是 receiver / dealer 的直屬子節點**，
  放深一層會靜默拿不到，而且沒有錯誤訊息。同理一顆 resolver 只吃**一顆** node。
- `_hittingEntity` 是 `[Component] public VarEntity`（`AbstractEffectEnterNode.cs`），
  可以指向別處的 VarEntity。所以**多個 receiver 要覆蓋同一個語意時，各自加一顆 node、
  `_hittingEntity` 全指到同一顆 VarEntity**（例：`Selectable Prompt ModulePack` 的
  `Distance Select` 與 `In Player Interact Range Detect 玩家互動範圍` 是 `Is BestMatched` 的 OR
  兩條路，只接一顆會在另一條點亮時讀到 null）。
- **共用同一顆 VarEntity 時 `_clearHittingEntityOnExit` 一律設 false**。清除是
  `OnEffectHitBestMatchExit → _bestEnterNode.ClearHittingEntityIfNeeded()`
  （`GeneralEffectReceiver.cs:186-193`），每顆 node 清的是**共用**的那顆 var ——
  A 退出就把 B 還在用的值清掉。留殘值是安全的：每次 best match enter 都會覆寫，
  而讀取端本來就用 `IsBestMatched`（`[Getter] Is BestMatched` / `IsBestMatchedReceiverCondition`）把關。
- 專案**沒有** `d_Selector` / `d_CurrentSelector` 之類的 varTag，也**沒有** receiver→dealer 方向的
  ValueSource；`up catalog getter` 只有反方向的 `GetBestMatchEntityFromDealer` /
  `ListEntityFromEffectDealer`。`IsBestMatchedReceiverCondition` 只讀 `_receiver.IsBestMatched`（bool），
  拿不到 entity。所以這條鏈只能自己接，不要再花時間找現成 getter。
- 既有範例：`Assets/0_Gameplay/0_Network Modules/Plug/[Entity] Socket for Plug 插座 (Receiver).prefab`
  的 `[Receiver] d_plug_Socketing/[Event] EffectEnterBestMatchNode/[Var] d_plug_Socketing hitEntity`
  （同一顆 var 也被 `[Event] EffectEnterNode._hittingEntity` 共用）；
  `Assets/0_Gameplay/0_Network Modules/[Part] 螺絲.prefab` 的螺絲起子提示分支。

## EffectHitTarget 共用 Enum

當 Action 需要選擇對 Dealer 或 Receiver 操作時，使用共用的 `EffectHitTarget` enum：

```csharp
// 位於 MonoFSM/1_MonoFSM_Core/Runtime/EffectHit/EffectHitTarget.cs
// namespace: MonoFSM.Runtime.Interact.EffectHit
public enum EffectHitTarget { Dealer, Receiver }
```

使用範例：
```csharp
public class MyEffectAction : AbstractArgEventHandler<GeneralEffectHitData>
{
    public EffectHitTarget _target = EffectHitTarget.Receiver;

    protected override void OnArgEventReceived(GeneralEffectHitData arg)
    {
        var resolver = _target == EffectHitTarget.Dealer
            ? arg.GeneralDealer
            : arg.GeneralReceiver;
        // 對 resolver 操作...
    }
}
```

**不要在各 Action 中重複定義 `{ Dealer, Receiver }` enum，統一使用 `EffectHitTarget`。**

## 通用 Action：VariableTransferAction

透過 `VariableTag` 跨實體傳輸 VarFloat 值，放在 `EffectEnterNode` 下使用。

- 繼承：`AbstractArgEventHandler<GeneralEffectHitData>`
- `_direction`：`DealerToReceiver`（預設）或 `ReceiverToDealer`
  - DealerToReceiver：從 Dealer entity 取 source → 應用到 Receiver entity
  - ReceiverToDealer：從 Receiver entity 取 source → 應用到 Dealer entity
- `_sourceVarTag`：`[SOConfig("VariableType")] VariableTag`，來源 VarFloat 的 tag（null 時用固定值）
- `_sourceValue`：`VarFloatWrapper`，當 `_sourceVarTag` 為 null 時使用的固定值
- `_targetVarTag`：`[SOConfig("VariableType")] VariableTag`，目標 VarFloat 的 tag
- `_operation`：`Add` / `Subtract` / `Set`
- `_multiplier`：float，傳輸時的乘數
- `_allowSelfTransfer`：bool，是否允許 Dealer == Receiver 時傳輸
- 腳本路徑：`MonoFSM-Pro/Runtime/EnemySystem/VariableTransferAction.cs`

**使用範例**：燃煤器 Dealer 偵測到煤炭 Receiver，`ReceiverToDealer` + `Add` 把煤炭的燃燒值加到燃煤器的 Fuel。

---

## 注意事項

- `_effectType` 必須兩邊相同才會觸發
- `AbstractArgEventHandler` 的 `_actionParent`（`[AutoParent] AbstractEventHandler`）需要有父層 EventHandler（即 `EffectEnterNode`）才能正常運作
- `OnActionExecuteImplement` 是舊有 FSM state 觸發用的，Event 驅動的 Action 不需要它，丟 `NotImplementedException` 即可
- 若不需 Dealer 追蹤 receiver 狀態，可直接呼叫 `receiver.OnEffectHitEnter(hitData)` 略過 dealer 流程

---

## 沒觸發時的診斷順序

先跑 `up effect-trace "<receiver 節點或其祖先>"`（**Play Mode**），它把下面整條鏈一次攤開。
需要手動查時照這個順序，每一段都是**靜默 return**，沒有 log：

| # | 段 | 看什麼 | 常見死因 |
|---|---|---|---|
| 1 | detector 偵測到 detectable | `EffectDetectable._debugDetectors` 有沒有那顆 detector | detectable 那側缺 kinematic Rigidbody（static-static trigger 不觸發）、collider 沒開 trigger、layer collision matrix 關著 |
| 2 | receiver 登記進 detectable 的 dict | `EffectDetectable.GetKeys` 含不含這個 effectType | receiver 不在 `EffectDetectable` 子樹下、`_effectType` 沒填 |
| 3 | dealer 有效 | `dealer.IsValid`、`_failReason` | dealer 底下的 `[If]` condition 不成立 |
| 4 | dealer ↔ receiver 配對 | `dealer.HasReceiverOverlap` / `receiver.HasDealerOverlap` | 兩邊 `_effectType` 不是同一顆 asset；dealer 掛在偵測範圍不夠的 detector 下 |
| 5 | **enterNode 的四道 gate** | `_lastSimulateEventTime`（-1 = 從沒跑過）、`_lastSkipReason` | 見下 |
| 6 | action | action 自己的 condition | sibling 順序、前面的 action 改掉了條件 |

第 5 段是 `AbstractEventHandler.EventHandleImplement`，四道 gate 依序是
`_conditionFolder.IsValid` → `_parentObj.IsCulling` → `gameObject.activeSelf` →
**`_parentObj.ShouldSimulte || _forceExecuteWithoutStateAuthority`**。
被擋下時會寫進 `_lastSkipReason` / `_lastSkipTime`（Editor only，`up peek` 和 Inspector 都看得到），
所以「事件有進來但 action 沒跑」直接讀這個欄位就有答案。

### `ShouldSimulte` 的判定與那個坑

`MonoObj.ShouldSimulte`：有 `ISimulateAuthorityProvider` → 看 state/input authority；
沒有但有 parent MonoObj → 繼承 parent；否則落到外部 push 的 `_shouldSimulateFlag`（預設 **false**）。

所以**場景上沒有 NetworkObject 的 root MonoObj，得靠 runner 在註冊時 push true**。
`LocalSimulatorRunner`（單機）是無條件 push；`FusionSimulatorRunner` 原本只 push 有 NetworkObject 的，
非網路的場景物件就會「單機正常、連線時整棵靜音」——現在兩邊都 push 了，遇到類似症狀先確認這段還在。
