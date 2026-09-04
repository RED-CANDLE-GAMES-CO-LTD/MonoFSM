# EffectHit Progress

- `TriggerEnterForDealerAndDetectable` 加上 `IsEnteredReceiver` guard，避免 dealer 剛變 valid 與 detectable 剛進入同 tick 成立時 enterNode 重複觸發兩次。
- `EffectDetector` 補上 `OnDisable` / condition 失效時的 `ClearAllDetections`，修正 detector 被關掉後不再 Simulate 導致 exit 永遠不發、`HasDealerOverlap` 卡在 true 的殘留問題。
- 新增 `ICullingEnterHandler`：`MonoObj` 對 `IsCulling` 做 latch 並廣播給自己 scope 的子樹，`EffectDetector` 收到就補送 exit，修正 culling 範圍比 trigger 範圍小時（ex: 瞬移讓 parent MonoObj 被 cull）整棵停止 tick 造成的漏更新。`HasDealerOverlap` 另外濾掉已失效的 dealer 當最後防線。
- `AbstractEventHandler` 的四道 early return 改成寫入 `_lastSkipReason` / `_lastSkipTime`（`[Conditional("UNITY_EDITOR")]`、常數字串、零 GC，另外走 `this.Log`），`TriggerEnterForDealerAndDetectable` 的 `receiver == null` 補 `SetFailReason` —— 之前這條鏈每一段都是靜默 return，「事件有進來但 action 沒跑」只能讀原始碼逐行對照。搭配新的 `up effect-trace`。
- `GeneralEffectReceiver` 新增 `IsBestMatched`（維護 `_bestMatchDealers`，比照 `HasDealerOverlap` 濾掉失效 dealer，overlap exit 也會一併移除），並新增 `IsBestMatchedReceiverCondition`，讓「這個 receiver 現在是不是 best match」可以被拉式查詢，不必只靠 enter/exit 事件推狀態。
- Culling 改為「凍結」語意，取代原本的 ClearAllDetections：cull 時不清 overlap / latch、不發 exit，resume 後仍在重疊的走 Stay（不重放 Enter）、離開的補 Exit；`OnDisable` 用新的 `MonoObj.IsCulledByHandle` 分辨「被 cull 連帶關掉」（凍結）vs「despawn／手動關掉」（照常清除）。resume 第一個 tick 有寬限（TriggerDetectorSource 的物理還沒餵資料，缺席不算離開）。對側 `EffectDetectable.IsSuspendedByCulling` 時同樣凍結不 exit。凍結期間被 Destroy 的走 `PurgeDestroyedReceivers` 靜默清；cull 期間的殘留查詢由 `IsValid`（含 IsCulling）擋，`HasReceiverOverlap` 比照 `HasDealerOverlap` 改為過濾失效 receiver。
- EffectDetector 加 _isResetGraceTick：reset 後第一個 detect tick 的重疊資料（物理還沒用還原後位置重跑）整批丟棄，避免插槽 mount / 傷害等 enter 副作用在 reset 當 tick 被誤重放
- 效能：`_conditions.IsAllValid()` 從收集迴圈（每個 result 一次）提到 `DetectUpdateCheck` 開頭只算一次（留在這層而非 Simulate，因為 `ManualEffectDetectAction` 會繞過 Simulate）；`ProcessDetectionChanges` 的 enter／stay 兩圈合併成一圈（順便修掉 Editor 下 `_lastDetectedObjects` 提前寫入導致同 tick Enter+Stay 的差異）；`HandleDealerStateChanges` 的 `new Dictionary` 改成重用欄位；`FindBestMatch` 的預設計分改 `sqrMagnitude`（單調，排序不變）並把 `transform.position` 提出迴圈、`_receivers.Count == 0` 早退；`_candidateReceivers.Add` 包成 `[Conditional("UNITY_EDITOR")]` 的 `AddCandidateReceiver`（純 debug 觀察用、且原本只加不清）

* `GeneralEffectDealer._isPassive`（2026-09-04）：「只偵測不施加效果」。開起來後 `EffectDetector` 照常判重疊、照常呼叫 `dealer.OnHitEnter/OnHitExit` 與 `OnBestMatchCheck()`，但不呼叫對面 receiver 的 `OnEffectHitEnter/Stay/Exit`（best match 的 receiver 側通知也一併跳過）。命中帳本（`_receivers` / `_hittingEntities` / `BestMatchReceiver`）完全由 dealer 自己在 enter/exit 維護，所以 `GetHittingEntities()`、`GetBestMatchEntityFromDealer` 對 passive dealer 照樣有值 —— 這是它存在的理由：讓「範圍內有什麼」和「什麼時候真的發效果」拆開，實際施加改由 `ForceTriggerEffectAction` 主動發。
  * **刻意不做**：passive 時 Stay 整段跳過（連 `dealer.OnHitStay` 也不跑）。receiver 端沒跑過 enter，`receiver.TryGetHitDataFor` 拿不到 hitData，硬要支援得另外在 dealer 側存一份，不值得。要每幀邏輯的用 dealer 自己的 Simulate。
  * 為什麼不用 `ManualEffectDetectAction`：那條把 detector 整顆切成手動（latch 一設不解除），Simulate 完全不判 → 「範圍內現在有沒有目標」根本沒有持續狀態可讀，做不出常駐的互動提示。passive 是「照常偵測、延後施加」，語意上才是對的那一刀。
  * 也不用 EffectZone 繞：zone 是純 pull、沒有 enter/exit，且要在每個可被觸發的物件上另外掛一顆 zone 並自己維護半徑，判定幾何和真正的 trigger 不一致（提示亮了卻按不動）。passive dealer 天生共用同一顆 collider。
  * `CanHitReceiver` 不看「是否已在 detected list」，所以 passive dealer 拿去 `ForceDirectEffectHit` 不會被自己的偵測狀態擋掉。
