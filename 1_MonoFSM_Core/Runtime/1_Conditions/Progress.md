# Progress

- 2026-09-25 新增 `IsViewRootMountedCondition`（道具的 ViewRoot 是否 mount 在別的 entity 上，可選 `_attachEntityTag`）。讀本地 ViewRoot 跟隨狀態，所以非 SA 端要有人套用 mount（NetworkedViewRoot 或 NetworkedBackpackSlot）才準；給偷竊怪的 scorer 和互動目標排除用，都在 SA / 本地判斷，不需要同步。
