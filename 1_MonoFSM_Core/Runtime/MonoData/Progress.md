# Progress

- `VarList.SetCurrentIndexTo` 放行 `-1`（新增 `AbstractVarList.NoSelectionIndex`）：-1 是「無選取」的合法狀態（GrabSlotHolder 空手、`GoToNext` 遇空 list 都會設 -1），原本會誤報 out of bounds。
