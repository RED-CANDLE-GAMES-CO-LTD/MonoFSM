# Progress

- 新增 `VarEntityCurrentItem`：掛在 VarListEntity 子物件即自動鏡射 `CurrentListItem`，外部可直接 ref 這顆，不必外部化 index 或接 `EntityFromListIndexProvider`；連帶把 `GenericUnityObjectVariable.GetValueInternal()` 開成 `protected virtual`。
- `VarList.SetCurrentIndexTo` 放行 `-1`（新增 `AbstractVarList.NoSelectionIndex`）：-1 是「無選取」的合法狀態（GrabSlotHolder 空手、`GoToNext` 遇空 list 都會設 -1），原本會誤報 out of bounds。
- `VarEntityCurrentItem` 補上 `Description` override（`CurrentItem<EntityTag>`），並在 MonoFSM skill 記錄「新 component 一律 override Description」的慣例與 `Rename()` 捷徑會蓋掉它的陷阱。
- `ICurrentEntityOwner` 加 editor-only 的 `DebugIteratedEntities`：`VarEntityCurrentItem` 在 Inspector 鏡射顯示 foreach 這一輪實際跑過的 entity 清單（跑完 Value 回 null 看不出軌跡），`VarListEntity` 回 null 不顯示。
