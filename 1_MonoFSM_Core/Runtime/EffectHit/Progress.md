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

## IgnoreColliderFilter 去掉 owner 注入（2026-09-05）
`_owner` / `Init(Component)` / `EditorRefreshPreview` 全砍掉，self entity 改成 `[AutoParent] MonoEntity[] _selfEntities`，
持有者只要在欄位上加 `[AutoNested]`。理由：owner 只是為了 runtime `GetComponentsInParent`，而 Auto 在 edit time 就能填好並
序列化 —— 順帶讓 Inspector 不用另做一份 preview 鏡像欄位、edit time 也看得到會忽略誰。
collider set 改 lazy build（`_isBuilt`），所以持有者連 `EnterSceneAwake` 樣板都不用寫。
**刻意不拆成獨立 component**：三個使用端都是各自私有的設定、沒有共享需求，拆了只是多一顆節點加一條會斷的 reference。

- Culling 凍結補到 dealer 的「讀值路徑」：第 6 條把 detector 端改成凍結語意（cull 時不清 overlap、不發
exit），但外部**查詢**命中狀態的那條路徑漏了。`GeneralEffectDealer.HasReceiverOverlap` 第一行是
`!isActiveAndEnabled` 就早退，而 culling handler 是直接 `SetActive(false)` 整個 LogicRoot 子樹 —— dealer
的 GO 跟著 inactive，於是 detector 那邊帳本明明凍著，`IsDealerHitAnyReceiverCondition` 讀到的卻是「沒打
到」。結果就是進出 culling 一次，值翻面兩次，下游 latch 全部重新判定（實例：鑽頭的 `d_NavMeshBlocking`
掛在會被 near culling 關掉的 LogicRoot 底下，玩家一走遠再回來就重跑一次撞擊判定）。
  現在只有「真的被關掉」（despawn／企劃手動 disable）才算無效，被 culling handle 關掉就沿用凍結期間的
overlap 值。區分方式刻意沿用 `EffectDetector.OnDisable` 的 `MonoObj.IsCulledByHandle`，兩處語意才不會分
岔 —— 「暫停模擬」和「東西不見了」必須是同一個判斷來源。
  **`EffectResolver.IsValid` 刻意不動**：它是 `isActiveAndEnabled && _conditions.IsAllValid()`，不含任何
culling 概念。凍結是「dealer 自己的帳本可不可以信」的問題，不是「這顆 resolver 現在有效嗎」的問題，混進
`IsValid` 會讓所有 effect 判定都吃到凍結語意（cull 中的東西還會被打到）。所以只在 dealer 的查詢入口處
理，原本那行說 `IsValid` 已含 `IsCulling` 的註解是錯的，一併改掉。
  診斷不用 log：查詢結果落在 `_overlapQueryState`（editor-only 賦值，`[Conditional]`），因為這是每幀被
getter 讀的路徑，字串或 log 都付不起。
