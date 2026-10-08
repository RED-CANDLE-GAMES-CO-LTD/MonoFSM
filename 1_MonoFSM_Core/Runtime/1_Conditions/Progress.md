# Progress

- 2026-09-25 新增 `IsViewRootMountedCondition`（道具的 ViewRoot 是否 mount 在別的 entity 上，可選 `_attachEntityTag`）。讀本地 ViewRoot 跟隨狀態，所以非 SA 端要有人套用 mount（NetworkedViewRoot 或 NetworkedBackpackSlot）才準；給偷竊怪的 scorer 和互動目標排除用，都在 SA / 本地判斷，不需要同步。

- 2026-10-06 新增 `RandomChanceCondition`（機率條件，`_probability` VarFloat）。走 TickRandom 而不是 UnityEngine.Random：同一 tick 重複判斷結果一致（Inspector / trace 再判一次不會翻盤），各端也一致。同 tick 要多顆獨立就各給不同 `_seed`。

- 2026-10-08 破關結算「host 按鍵回大廳」用現成的 `InputActionWasPressedCondition` 接玩家的 Interact（GGameplayUI `Get<Player>` 底下的 `d_Interact Input` VarMonoInput proxy），不另寫寫死 key 的 condition：MonoInputAction 走 Fusion networked input，WasPressed 每個 forward tick 判一次、可 rollback，能改綁鍵；寫死 key 在 Simulate 裡判斷時一個 frame 可能跑 0 或多個 tick，得自己靠 `CheatKeyLatch` 補漏按 / 重複觸發。（當天寫過一版 `WasKeyPressedCondition`，已刪除。）
