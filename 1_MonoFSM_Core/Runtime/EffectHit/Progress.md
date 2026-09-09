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

- 凍結補完第二道閘門，並劃清「查詢」與「能不能被打到」的界線：第 7 條只治了 dealer 自己 GO inactive
那一層，`HasReceiverOverlap` 迴圈裡還有第二道 `receiver.IsValid`，而 `EffectResolver.IsValid` 一樣不看
culling —— 所以值照樣翻面。**只治一側是無效的**：dealer 與 receiver 由同一組 culling observer 驅動
（同一個 group key、同一個距離 band、radius 都是 3），空間上相鄰的兩顆必然同時被關掉，鑽頭 vs Large
Rotten Trunk 就是這個關係，兩邊的 receiver / dealer 都掛在各自母 prefab 的 `LogicRoot` 底下、都在
`Near Logic Activate` 的關閉清單裡。
  切法是新增 `EffectResolver.IsValidOrFrozenByCulling`（＋`IsSuspendedByCulling`，寫法沿用
`EffectDetectable` 那顆），`IsValid` 一個字都不動。界線是：**「查詢命中狀態」用前者，「能不能被打到」
用後者**。`CanHitReceiver` 刻意留在後者 —— 被 cull 的物件不該收到 effect，這是一開始就定下的語意，把凍結
混進 `IsValid` 會直接違反它。cull 期間仍可安全評估 conditions，因為 `ConditionHelper.IsAllValid` 對
inactive 的 condition 是跳過不計、不是判 false。
  `HasDealerOverlap` / `IsBestMatched` 是同一個病灶的對稱面，一起改；三處的 fail reason 收在共用的
`EffectResolver.OverlapQueryState`（editor-only 賦值），receiver 兩個入口各記一份，否則每幀互相蓋掉。
  **順帶記一件很容易誤判的事**：這套 culling 完全是 client-local —— pivot 是 `PPlayer` 上的
`CullingGroupProxy`（距離參考點就是 player 自己，bands 10/30/50、near 門檻 2），而且同一台機器上每個
player 實例（含 remote proxy）都是 contributing observer，任一玩家夠近就不 cull。所以「host 正常、client
會重新觸發」這種現象**不是**網路同步或 authority 問題，只是那台機器上剛好沒有玩家在範圍內。查這類 bug
不要先往 Fusion 那邊找（AOI 也沒開在這些物件上）。
  **已知未解、需 runtime 判定**：resume 那一刻 `EffectDetector` 只給一 tick 寬限
（`_isResumeGraceTick`），而 `TriggerDetectorSource` 要等物理重新餵 `OnTriggerStay`。若一 tick 不夠，除了
讀值翻面之外還會多一次真正的 exit→enter 重放，那是獨立成因，本次沒有處理，也推不出來，要實機看。

- 凍結的「結束時機」才是這一系列問題的結構性根源：第 8 條把凍結期間的讀值治好了，但一離開凍結馬上又壞。
`_isResumeGraceTick` 原本在每次 `DetectUpdateCheck()` 結尾**無條件**清掉（註解寫「寬限只有一個
tick」），而一個 tick 不夠 —— resume 那一 tick 物理還沒帶著剛 enable 的 collider 跑過，帳本是空的；grace
被消耗後，下一 tick 的缺席就被判成離開，於是送出**真 exit**。這不只是 getter 翻面：
`dealer.OnHitExit` 會清 `_receivers`、跑 `_exitNode.EventHandle()`、`_hittingEntities.Remove`、
在 `_receivers` 歸零時 `ClearHittingEntityIfNeeded()`，下游 enter/exit 節點上的 action 整輪重放。症狀就是
玩家一走近，state 瞬間 idle 再回 stop。
  **所以不能在 condition 或 VarBool 層擋一帧** —— 那只遮得住讀到的值，遮不住事件與 var 寫入這一半。修必須
落在「exit 到底要不要送出」這一層。
  現在 grace 改成有條件消耗：帳本有東西（物理確實餵過）才正常消耗；resim tick 一律續命；而 bounded 的退出
條件是「物理時間已經前進過」。**刻意不用 tick 數或 frame 數**：`DetectUpdateCheck` 由
`FixedUpdateNetwork` 驅動，resim 時一個 frame 跑多個 tick、物理只步進有限次（本檔 `SimulateOrder` 上方
早就有註解講這個坑，只是當時沒拿它保護 grace），tick 數會超前物理，拿來當條件等於換一種猜法。只有物理時鐘
跟 `OnTriggerStay` 的來源同步。觀測的 fixedTime 要等 resume 後**第一個** detect tick 才記，不能在
`OnCullingEnter` 記 —— cull 可能持續好幾秒，那時記下的值早就被超過，條件會在 resume 第一 tick 就成立、
等於沒改。
  真 despawn／手動 disable 那條路不受影響：`ClearAllDetections` / `ResetStateRestore` 照舊直接清 grace
並送出 exit，這次只是順手把 fixedTime 的觀測 latch 一起歸零（despawn→respawn 復用同一顆時，殘留的觀測會
讓 bounded 條件套用到上一輪的時間）。
  **值得寫給下一隻 agent 的結構性問題**：`MonoObj.CullingStateCheck()` 只在**進入** cull 的邊緣廣播
（`if (!isCulling) return;`，註解寫「變回可見不用特別做事」），框架**沒有 resume 通知**。所以凍結的結束
條件永遠是「GO 變 active」這個零緩衝的瞬時判斷，而 culling handler 是直接 `SetActive` 整棵子樹 —— 任何
「靠物理／靠外部每帧餵資料」的機制在 resume 邊緣都會有一段空窗。之後再遇到類似的 resume 抖動，先想「這個
機制的資料來源在 resume 後幾個 step 才會回來」，而不是去找網路同步。
  **已知未做**：(a) 選項 2 —— 把「資料新不新鮮」下沉到 `AbstractDetectionSource`（加
`HasFreshPhysicsData`，ray/overlap 類 source 天生 fresh、不需要 grace），比現在在 detector 用時鐘推論精準，
但要動 3 支檔案；(b) `Condition/IsEffectDealerOrReceiverCondition.cs:91`（`CheckMode.IsValidNow`）是第
8 條那組同病灶的第四處，仍在用 `IsValid`，cull 期間一樣會翻面。

- 「缺席」不等於「離開」：exit 判定改成證據型。第 9 條處理的是「**自己** resume 後物理還沒餵資料」，
但**對側** resume 是對稱的同一個窗口，而且完全沒有機制蓋住 —— `IsSuspendedByCulling` 是零緩衝的瞬時值，
對側 handle 一 `SetActive(true)` 它就翻 false。
  **核心時序（這一整串問題的關鍵，務必先看懂再改這段程式）**：culling handler 的
`SetActive(true)` 由 CullingGroup 的 callback 觸發，跑在 render 階段、FUN 之外；而 detector 的
`Simulate` 排在 `RunnerSimulatePhysics` **之前**（order -500 vs 0，見本檔上方 `SimulateOrder` 的註解）。
所以對側 collider 剛 enable 的那個 tick，detector 讀到的 `OnTriggerStay` 是**上一個 physics step** 的
結果 —— 那個 step 裡對側還 inactive，缺席是必然的、不代表離開。**任何「對側剛從 inactive 變 active」的
情境，第一個 tick 的缺席都不能當離開。**
  修法（D1）：凍結過的條目，exit 只能由「對側已 active 且又缺席第二次」觸發。記帳放在 `DetectData`
這顆 struct 的兩顆 bool（`WasFrozenByCulling` / `SeenActiveButAbsent`），零 GC；**清除點天然正確** ——
物理重新偵測到時收集迴圈是 `new DetectData(...)` 整顆覆蓋，旗標自動歸零，不需要顯式清除也就漏不掉。
只有凍結過的條目走這條，從未被 cull 的正常 enter/exit 一 tick 都不延後。
  **為什麼是證據型而不是時鐘型**：前一版（第 9 條）用 `Time.fixedTime` 有沒有前進當退出條件，那是在
假設 Simulate 與物理的先後順序，而 resim 一個 frame 跑多個 tick 又讓假設失效。更慘的是那個實作本身有
bug：主條件寫成 `_thisFrameDetectedObjects.Count > 0`，判斷點卻在 `ProcessDetectionChanges` 之後，而
carry 分支寫入的就是那個容器 —— **carry 自己把條件餵飽，`ConsumedByPhysics` 那格從頭到尾在說謊**。
教訓：診斷欄位（和判斷條件）如果讀的是會被同一段流程改寫的容器，它就只會告訴你你想聽的話。時間條件
整組已刪除，`_isResumeGraceTick` 保留但回到**無條件消耗** —— 它只需負責自己 resume 的那第一個 tick，
跨 tick 的責任由 D1 接手；grace 期間 carry 出去的條目會被標成凍結序列，D1 才有依據。
  **第三條 exit 出口**：`CheckDealerStateChanges` / `HandleDealerStateChanges` 用 `dealer` 的有效性做
latch，變無效就從 `ProcessDealerStateChangesForDetectable` 直接送 exit。這條跑在
`ProcessDetectionChanges` **之前**，不看 grace 也不看 `IsSuspendedByCulling`，**D1 蓋不到它** ——
所以那兩處 latch 改用 `IsValidOrFrozenByCulling`（第 8 條那顆）。注意
`TriggerEnterForDealerAndDetectable` 的 gate 仍是 `IsValid`：那問的是「能不能真的被打到」，cull 中的
物件不該收到 effect，這條界線從第 8 條起就沒變。
  界線複查（都還分得開）：真 Destroy 走 `detectable == null` → `PurgeDestroyedReceivers`；企劃手動
disable 時 culling handle 仍 active → `IsSuspendedByCulling` 為 false、`WasFrozenByCulling` 也是
false → 走正常 exit，不延後。
  **已知未做**：(a) 選項 2 —— 把「資料新不新鮮」下沉到 `AbstractDetectionSource`（`HasFreshPhysicsData`，
ray/overlap 類 source 天生 fresh、不需要任何寬限）；(b)
`Condition/IsEffectDealerOrReceiverCondition.cs:91`（`CheckMode.IsValidNow`）仍用 `IsValid`，是第 8 條
那組同病灶的第四處，cull 期間一樣會翻面。
- 2026-09-09 `BaseEffectDetectTarget` 加 `_detectableOverride`：`_detectable` 是 `[AutoParent]`，只往上找，而 `AutoAttributeManager` 的 `SetValue` 無條件覆寫（含 null），手填 `_detectable` 在 pool 生成 / 存檔時會被洗掉。拆件模組把 anchor collider 生成在宿主 view 底下（不在模組子樹裡），要打進模組的 receiver 只能靠這顆顯式 override。`Detectable` property 優先回 override，兩顆都 null 才算錯。
