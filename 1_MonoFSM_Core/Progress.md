# Progress

- 新增 `SetVarFloatToCurrentTimeAction` / `SinceVarFloatTimeStampCondition`：用 VarFloat 記時間戳＋算時間差做冷卻，不需要 timer 倒數。
- 新增 cheat 瞬移：`CheatTeleportPoint`（場景 marker，自動排序＋Gizmo 標號）＋ `CheatTeleportPoints`（Alt+1~9 / Alt+T，透過子節點的 `IArgEventReceiver<Vector3>` 瞬移玩家）。
