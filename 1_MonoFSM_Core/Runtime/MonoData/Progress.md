# Progress

- 新增 `VarEntityCurrentItem`：掛在 VarListEntity 子物件即自動鏡射 `CurrentListItem`，外部可直接 ref 這顆，不必外部化 index 或接 `EntityFromListIndexProvider`；連帶把 `GenericUnityObjectVariable.GetValueInternal()` 開成 `protected virtual`。
- `VarList.SetCurrentIndexTo` 放行 `-1`（新增 `AbstractVarList.NoSelectionIndex`）：-1 是「無選取」的合法狀態（GrabSlotHolder 空手、`GoToNext` 遇空 list 都會設 -1），原本會誤報 out of bounds。
- `VarEntityCurrentItem` 補上 `Description` override（`CurrentItem<EntityTag>`），並在 MonoFSM skill 記錄「新 component 一律 override Description」的慣例與 `Rename()` 捷徑會蓋掉它的陷阱。
- `ICurrentEntityOwner` 加 editor-only 的 `DebugIteratedEntities`：`VarEntityCurrentItem` 在 Inspector 鏡射顯示 foreach 這一輪實際跑過的 entity 清單（跑完 Value 回 null 看不出軌跡），`VarListEntity` 回 null 不顯示。
- 修正動態 mount（神像插台座等）reset 後不回原點：LocalTransformResetter 的 skip 條件從 AttachToViewRoot 改成 ViewRoot.IsSceneStartAttached（只有 SceneStart 就 nested 的才跳過）
- ViewRoot 加「關卡初始 mount baseline」：插槽/台座類的 mount（`MountViewRootAction._isResetBaselineMount` 勾起來）在 `MountTo` 時記下 target / mount point / handlePhysics / disableColliders，`ResetStateRestore` 的動態 mount 分支改成「有 baseline 就掛回去，沒有才 ClearFollowTarget」。為什麼需要：`_ignoreStartReparent = True` 的物件永遠拿不到 SceneStart baseline（`EnterSceneStart` 第一行就 return），它的「原位」其實是 mount point 決定的，只靠 LocalTransformResetter 只會回到 authored transform（神像案例差 0.212m / 35.68°，而且它是 kinematic 不會被物理修正）。
- **為什麼用旗標而不是「自動把第一次 MountTo 當 baseline」**：抓取、Dock、投擲吸附全走同一條 `MountTo`，玩家先把物件抓起來再放回去也會是「第一次」，自動判斷會把玩家的操作記成關卡初始狀態，reset 後物件就黏回玩家放的地方。所以由關卡端的 action 明確宣告。
- 還原**不依賴 EffectDetector 重新產生 Enter**：detector reset 後要吃掉一個 grace tick（見 EffectDetector 的 `_isResetGraceTick`）才會補放 enter，靠它重掛會有「先跳回 authored pose、幾 tick 後才被吸回插槽」的閃動；baseline 自己掛回去就沒有這段。
- baseline 還原只在 SA 端做（`IsMountAuthority` 走 `BindEntity.BindObj.HasStateAuthority`）：`MountTo` 會改 Rigidbody 的 kinematic，那是 SA 的職責，proxy 的 kinematic 歸 NetworkRigidbody 管。proxy 維持 `ClearFollowTarget()`，再由 `NetworkedViewRoot` 把 SA 的 mount 狀態套回來。
- baseline 刻意跨 reset 保留（`ClearFollowTarget` / `Unmount` / `ResetStateRestore` 都不清），它記的是關卡初始狀態不是當下狀態。
