# EffectHit Progress

- `TriggerEnterForDealerAndDetectable` 加上 `IsEnteredReceiver` guard，避免 dealer 剛變 valid 與 detectable 剛進入同 tick 成立時 enterNode 重複觸發兩次。
- `EffectDetector` 補上 `OnDisable` / condition 失效時的 `ClearAllDetections`，修正 detector 被關掉後不再 Simulate 導致 exit 永遠不發、`HasDealerOverlap` 卡在 true 的殘留問題。
- 新增 `ICullingEnterHandler`：`MonoObj` 對 `IsCulling` 做 latch 並廣播給自己 scope 的子樹，`EffectDetector` 收到就補送 exit，修正 culling 範圍比 trigger 範圍小時（ex: 瞬移讓 parent MonoObj 被 cull）整棵停止 tick 造成的漏更新。`HasDealerOverlap` 另外濾掉已失效的 dealer 當最後防線。
- `AbstractEventHandler` 的四道 early return 改成寫入 `_lastSkipReason` / `_lastSkipTime`（`[Conditional("UNITY_EDITOR")]`、常數字串、零 GC，另外走 `this.Log`），`TriggerEnterForDealerAndDetectable` 的 `receiver == null` 補 `SetFailReason` —— 之前這條鏈每一段都是靜默 return，「事件有進來但 action 沒跑」只能讀原始碼逐行對照。搭配新的 `up effect-trace`。
