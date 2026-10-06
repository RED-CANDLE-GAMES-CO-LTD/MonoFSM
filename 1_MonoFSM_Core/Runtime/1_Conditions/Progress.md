# Progress

- 2026-09-25 新增 `IsViewRootMountedCondition`（道具的 ViewRoot 是否 mount 在別的 entity 上，可選 `_attachEntityTag`）。讀本地 ViewRoot 跟隨狀態，所以非 SA 端要有人套用 mount（NetworkedViewRoot 或 NetworkedBackpackSlot）才準；給偷竊怪的 scorer 和互動目標排除用，都在 SA / 本地判斷，不需要同步。

- 2026-10-06 新增 `RandomChanceCondition`（機率條件，`_probability` VarFloat）。走 TickRandom 而不是 UnityEngine.Random：同一 tick 重複判斷結果一致（Inspector / trace 再判一次不會翻盤），各端也一致。同 tick 要多顆獨立就各給不同 `_seed`。
