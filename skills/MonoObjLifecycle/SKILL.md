---
name: MonoObjLifecycle
description: MonoObj 更新生命週期系統的使用指南。當需要：(1) 了解 WorldUpdateSimulator 的更新迴圈架構 (2) 實作 Simulate、Render 等每幀更新邏輯 (3) 新增 IUpdateSimulate、IBeforeSimulate、IAfterSimulate、IRenderUpdate 實作 (4) 理解 MonoObj 註冊/反註冊流程 (5) 理解 local FixedUpdate/LateUpdate 與 Fusion FixedUpdateNetwork/Render 時機 (6) 拆分 simulation/render culling 時使用此 skill。
---

# MonoObj Lifecycle

MonoObj 的每幀更新透過 `WorldUpdateSimulator` 集中管理，以介面驅動方式分階段執行。

## 執行時序

```
LocalSimulatorRunner.FixedUpdate / FusionSimulatorRunner.BeforeTick + FixedUpdateNetwork
├── World.BeforeSimulate(...)               → IBeforeSimulate
├── World.Simulate(...)                     → IUpdateSimulate
└── World.AfterSimulate(...)                → IAfterSimulate

LocalSimulatorRunner.LateUpdate / FusionSimulatorRunner.Render
└── World.Render(...)                       → IRenderUpdate

LocalSimulatorRunner.LateUpdate 尾段 / FusionSimulatorRunner.AfterRender
└── World.AfterRender()                     → IAfterRenderMono
```

每顆註冊的 `MonoObj` 都是獨立 scope。`[AutoChildren(StopAtType = typeof(MonoObj))]`
不會跨進 nested MonoObj；nested MonoObj 會自己註冊並被 WorldUpdateSimulator 呼叫，不是由 root
遞迴代跑。

## 更新介面

所有介面定義於 `MonoFSM/1_MonoFSM_Core/Runtime/LifeCycle/Update/Simulate/IUpdateSimulate.cs`。

| 介面 | 方法 | 時機 | 用途 |
|------|------|------|------|
| `IBeforeSimulate` | `BeforeSimulate(float deltaTime)` | FixedUpdate 開頭 | 輸入處理、前置計算 |
| `IUpdateSimulate` | `Simulate(float deltaTime)` | FixedUpdate 主體 | 核心模擬邏輯 |
| `IAfterSimulate` | `AfterSimulate(float deltaTime)` | FixedUpdate 尾段 | 後處理、同步 |
| `IRenderUpdate` | `Render(float runnerLocalRenderTime)` | Local LateUpdate / Fusion Render | 視覺更新、插值、動畫；不吃 `ShouldSimulte` authority gate |
| `IAfterRenderMono` | `AfterRender()` | Fusion AfterRender / local render 尾段 | 必須晚於一般 Render 的視覺收尾 |

## 實作模式

實作任一介面並掛在 MonoObj 子物件上，會被 `[AutoChildren]` 自動收集。

```csharp
public class MyVisualUpdater : MonoBehaviour, IRenderUpdate
{
    public void Render(float deltaTime)
    {
        // 視覺插值、動畫更新等
    }
}
```

```csharp
public class MySimulator : MonoBehaviour, IUpdateSimulate
{
    public void Simulate(float deltaTime)
    {
        // 核心模擬邏輯
    }

    // 可選：控制執行順序（數字越小越先）
    public int SimulateOrder => 10;
}
```

## 關鍵檔案

| 檔案 | 職責 |
|------|------|
| `WorldUpdateSimulator.cs` | 世界更新中心，管理所有 MonoObj 的註冊與每幀迭代 |
| `LocalSimulatorRunner.cs` | 本地模式的 Runner，驅動 FixedUpdate/LateUpdate |
| `MonoObj.cs` | 持有各階段介面陣列，轉發更新呼叫 |
| `IUpdateSimulate.cs` | 所有更新介面定義 |

## Culling phase gate

`MonoObj` 支援三種 handle，三者都由自己 scope 內的 `[AutoChildren(StopAtType = MonoObj)]`
自動收集：

| Handle | 停止的 phase | 用途 |
|---|---|---|
| `CullingActiveHandle` | Simulate + Render 全部 | legacy 相容；只有真的要整顆暫停才用 |
| `SimulationCullingActiveHandle` | BeforeSimulate / Simulate / AfterSimulate | 距離型 gameplay、AI、sensor 成本控制 |
| `RenderCullingActiveHandle` | Render / AfterRender | Renderer、VFX、Animator 等本機視覺成本控制 |

`MonoObj.IsCulling` 為既有 gameplay 相容介面，等同 `IsSimulationCulling`。Render 專用判斷看
`IsRenderCulling`。parent 的 simulation/render culling 會分 phase 傳給 nested MonoObj；
`_isIgnoreParentObjCulling` 會同時切斷兩種 parent phase 繼承。

共用 module `Packages/com.monofsm.pro/Prefabs/Prefab Modules/Culling Event Target.prefab` 的標準串法：

- `NearOnly` → `SimulationCullingActiveHandle`
- `Visible OR Near` → `RenderCullingActiveHandle`

`CullingEventTarget`、root `MonoObj`、`NetworkObject` / `NetworkBehaviour`、同步用 Var/FSM 與
`RenderLoopHandler` 本身留在 always-on shell，不要放進被 `CullingTargetGameObjects` 關閉的 root。
CullingGroup visibility 是各 peer 的 camera-local 結果，只能控制本機 scheduling/visual，不能拿來改
network state 或 authority。

## 注意事項

| 項目 | 說明 |
|------|------|
| **nested MonoObj 獨立更新** | 每顆 nested MonoObj 都要註冊；StopAtType 讓各 scope 不重複收集 loop component |
| **Proxy 的 logic / visual 分流** | `Simulate` 由 `ShouldSimulte`（State/Input Authority）擋；`Render` 不吃 authority gate，所以 non-simulated proxy 也能更新本機視覺 |
| **IsReady 檢查** | WorldUpdateSimulator 在 `WorldInit()` 後才開始更新 |
| **TimeScale** | 透過 `WorldUpdateSimulator.DeltaTime` 取得含 TimeScale 的 deltaTime |
| **Simulate vs Render** | local 為 FixedUpdate/LateUpdate；Fusion 為 FixedUpdateNetwork/Render。不要把 Render 寫成依賴 local camera 的 authoritative logic |
