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
