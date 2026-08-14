# 撰寫 MonoFSM Action / Condition C# 腳本

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

### Render behaviour 掛在哪 → 決定何時觸發（多人時決定 client 跑不跑）

`AbstractRenderBehaviour`（`IRenderBehaiour`）是被**父物件收集後代呼叫**的，同樣一顆元件掛在不同位置，觸發頻率完全不同：

| 掛載位置 | 收集者 | 觸發 |
|---|---|---|
| `[State] Xxx` 節點**直接底下** | `AbstractStateBehaviour._renderActions`（`[AutoChildren(DepthOneOnly)]`） | 該 state active 時**每 render frame** `OnRender()`；進入時 `OnEnterRender()`。**自帶狀態範圍，不用再加 `IsStateCondition`** |
| `[Event] OnStateUpdate` 等 event handler 底下 | `AbstractEventHandler._renderActions` | **只有 `EnterRenderInvoke()`** —— state enter 時呼叫一次 `OnEnterRender()`，**沒有每幀的 render invoke**。把每幀邏輯放這裡會靜靜地不跑 |
| `[Event] RenderLoop`（`RenderLoopHandler`）底下 | 同上，但 handler 自己實作 `IRenderUpdate.Render()` | 每 render frame，**無狀態範圍**，要自己用 `_conditionGroup` 把關 |

「這個狀態期間每幀套用」的東西（骨骼朝向覆寫、beam、IK、跟隨）→ 掛 `[State]` 節點底下，最單純。

**多人時這是 client 端唯一會跑的路徑**：`MonoObj.Simulate()` 在 `!ShouldSimulte` 時整棵子樹直接 return，`AbstractEventHandler` 也有同一道 gate（`_forceExecuteWithoutStateAuthority` 沒用，被 MonoObj 擋在更外層），所以 proxy 上 Action / RaycastCache / timer 全部不執行；只有 Render / AfterRender 兩個 phase 兩端都跑。

推論：**凡是兩端都要看到的持續性視覺，套用端必須是 render behaviour，而它讀的資料要嘛掛 `NetworkedVarTag` 從 SA 同步過來，要嘛在 render 端本地重算。** 只同步資料而套用端還留在 Action（simulate）底下，症狀是「host 正常、client 的視覺不動或指錯方向，但判定是對的」。

實例：噴水怪 `1_Enemy 噴水怪 Variant` 的水槍 —— `HeadLookAtAnimatorApplier` 從 `OnStateUpdate` 下的 Action 改成掛在 `[State] Shoot Attack` 底下的 `AbstractRenderBehaviour`，並把 `HeadForward Out`（瞄準方向）與 `hitPosVar`（RaycastCache 的落點、beam 終點）兩個 `VarVector3` 加 `NetworkedVarTag` 進 `NetworkedVarSyncArray._syncVector3s`。

### 同一功能要同時支援 Action 與 Render

`AbstractStateAction`（掛 `IActionParent`、event 觸發）與 `AbstractRenderBehaviour`（掛 `IRenderInvoker`、每 render frame 觸發）**都繼承 `AbstractDescriptionBehaviour`，但父物件契約與 Condition 系統完全不同**（Action 用 `_conditions[]`，Render 用 `_conditionGroup`，`HasError` 各自檢查父型別）。

C# 單一繼承，**不要在同一個 class 上 implement `IRenderBehaiour` 硬兼容兩者**：不管選哪個 base，另一邊的 `HasError` 會因父物件型別不符而報錯，掛載位置被綁死。

正解：**把核心邏輯抽成一個 `[Serializable]` class，做兩個薄 wrapper**：

```csharp
[Serializable]
public class XxxWriter // 共用邏輯 + 欄位 + Description
{
    public string Description => ...;
    public void Write(Object byWho) { ... }
}

public class XxxAction : AbstractStateAction
{
    [HideLabel] [InlineProperty] public XxxWriter _writer = new();
    public override string Description => _writer.Description;
    protected override void OnActionExecuteImplement() => _writer.Write(this);
}

public class XxxRender : AbstractRenderBehaviour
{
    [HideLabel] [InlineProperty] public XxxWriter _writer = new();
    public override string Description => _writer.Description;
    public override void OnEnterRenderImplement() => _writer.Write(this);
    public override void OnRenderImplement() => _writer.Write(this);
}
```

`[HideLabel][InlineProperty]` 讓 writer 欄位在 Inspector 攤平，看起來就像直接寫在 wrapper 上。

**參考實作**：`1_MonoFSM_Core/Runtime/Action/VariableAction/`（`PositionToVarVector3Writer` + `SetVarVector3FromTargetAction` / `SetVarVector3FromTargetRender`）
