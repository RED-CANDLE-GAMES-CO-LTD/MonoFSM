---
name: MonoObjLifecycle
description: MonoObj 更新生命週期系統的使用指南。當需要：(1) 了解 WorldUpdateSimulator 的更新迴圈架構 (2) 實作 Simulate、Render 等每幀更新邏輯 (3) 新增 IUpdateSimulate、IBeforeSimulate、IAfterSimulate、IRenderSimulate 實作 (4) 理解 MonoObj 註冊/反註冊流程 (5) 理解 FixedUpdate/LateUpdate 的執行時機時使用此 skill。
---

# MonoObj Lifecycle

MonoObj 的每幀更新透過 `WorldUpdateSimulator` 集中管理，以介面驅動方式分階段執行。

## 執行時序

```
FixedUpdate (LocalSimulatorRunner)
├── BeforeSimulate(time, deltaTime, tick)   → IBeforeSimulate
├── Simulate(deltaTime)                      → IUpdateSimulate
│   ├── (per MonoObj) BeforeSimulate
│   ├── (per MonoObj) Simulate              ← 依 SimulateOrder 排序
│   └── (per MonoObj) AfterSimulate         → IAfterSimulate

LateUpdate (LocalSimulatorRunner)
├── Render(deltaTime)                        → IRenderSimulate
└── AfterUpdate()
```

## 更新介面

所有介面定義於 `MonoFSM/1_MonoFSM_Core/Runtime/LifeCycle/Update/Simulate/IUpdateSimulate.cs`。

| 介面 | 方法 | 時機 | 用途 |
|------|------|------|------|
| `IBeforeSimulate` | `BeforeSimulate(float deltaTime)` | FixedUpdate 開頭 | 輸入處理、前置計算 |
| `IUpdateSimulate` | `Simulate(float deltaTime)` | FixedUpdate 主體 | 核心模擬邏輯 |
| `IAfterSimulate` | `AfterSimulate(float deltaTime)` | FixedUpdate 尾段 | 後處理、同步 |
| `IRenderSimulate` | `Render(float deltaTime)` | LateUpdate | 視覺更新、插值、動畫 |

## 實作模式

實作任一介面並掛在 MonoObj 子物件上，會被 `[AutoChildren]` 自動收集。

```csharp
public class MyVisualUpdater : MonoBehaviour, IRenderSimulate
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

## 注意事項

| 項目 | 說明 |
|------|------|
| **HasParent 跳過** | 巢狀 MonoObj（有 parent）不執行更新，由 root MonoObj 統一管理 |
| **IsProxy 跳過** | Proxy 物件（網路遠端）跳過本地模擬 |
| **IsReady 檢查** | WorldUpdateSimulator 在 `WorldInit()` 後才開始更新 |
| **TimeScale** | 透過 `WorldUpdateSimulator.DeltaTime` 取得含 TimeScale 的 deltaTime |
| **Simulate vs Render** | Simulate 在 FixedUpdate（物理幀）；Render 在 LateUpdate（渲染幀） |
