# Progress

## Best Match 評分器（AbstractOnlyTriggerBestMatch 家族）

- `GeneralEffectDealer.FindBestMatch()` 沒掛 scorer 時是「離 dealer 最近的 receiver 勝出」，
  掛了 scorer 就整條交給 `CalculateScore`。scorer 是 `[Auto]` 抓同 GameObject，所以「換演算法」
  的操作單位是「哪一顆 dealer 節點」，不是全域設定。這是刻意的：同一個玩家身上不同 dealer
  （近戰範圍 / 瞄準射線 / 抓取）想要的排序邏輯本來就不一樣。

- **為什麼不直接改 `DefaultBestMatchScorer`**：它掛在 `Base Character.prefab` 上、被全專案
  所有 dealer 繼承（PPlayer 上就 11 顆），是「priority + 距離」這個最保守語意的預設值。
  在它裡面加視線項＝一次改掉所有互動、抓取、滅火、霧區判定的手感，而且那些 dealer 大多
  沒有射線來源可指，只會多一堆「引用沒指」的無聲退化。新演算法一律開新子類、由 prefab 決定
  誰要用。

- `AimAlignedBestMatchScorer`：在 default 的公式上加 `dot(視線方向, 指向 receiver 的方向)` 一項，
  三項權重全開成 serialized 欄位。預設 `priority=1000 / aim=10 / distance=1`，用意是
  「視線主導排序、距離退居同視線角下的 tiebreaker、priority 仍然絕對優先」。
  距離項保留而非拿掉：兩顆 receiver 在視線上前後排開時，只有距離能分出勝負。

- **為什麼視線分低於 `_minAimDot` 時只歸零、不回 `float.MinValue`**：
  「這顆 receiver 該不該進候選」是 `EffectResolver._conditions`（receiver 節點下的 `[If]`）
  和 `EffectDetectable._interactConditions` 的職責，在 `GeneralEffectDealer.CanHitReceiver`
  就會被剔除。scorer 只負責排序 —— 若 scorer 也能「排除」，同一件事就有兩個真相來源，
  而且會出現「候選清單裡明明有東西，best match 卻是 null」這種靜態看不出來的狀況。
  歸零的語意是「背後的東西不吃視線加分，但如果貼在臉上（距離項）還是可能被選中」。

- **射線來源刻意用 `Transform` 而不是 `AbstractCastCache`**：
  `AbstractCastCache` / `RaycastCache` / `AbstractRayProvider` 都在 `MonoFSM_Physics`
  assembly，而 `MonoFSM_Physics` 是 reference `MonoFSM.Core.Runtime` 的方向（Core 不能反向
  依賴 Physics）。scorer 家族住在 Core.Runtime，拿不到 `CachedRay`。
  替代方案是可行的：`AbstractCastCache.TryCast()` 每次 cast 都會做
  `transform.rotation = Quaternion.LookRotation(cachedRay.direction)`，所以把
  `_aimRayTransform` 指到 `[Raycast] RaycastCache` 節點，`forward` 就是當幀的視線方向。
  刻意不做的事：不為了拿一條 ray 就把 Physics 拉進 Core 的 references（會讓 Core 不再是
  最底層），也不用 `[AutoChildren]` 猜結構（跨節點引用一律手動指）。

- 除錯走 `[ShowInInspector]`（`_lastState` / `_debugBestReceiver` / `_debugBestAimScore` /
  `_debugBestDistance` / `_debugBestScore`），不走 log。`CalculateScore` 是每個 receiver
  每次判定都會進的熱路徑，log 只留給「引用沒指」「forward 是 0」這種一次性設定錯誤，且用
  bool latch 保證只吼一次。`_debugPassFrame` 用 `Time.frameCount` 判斷是不是新的一輪計分，
  才能在 Inspector 上顯示「這一輪誰贏」而不是「歷史最高分」。

- `BoolVarFilteredBestMatchScorer`（2026-09-15）：繼承 Default，先用 `_boolVarTag` 讀 receiver entity 的 VarBool，
  不等於 `_expectedValue` 就回 `float.MinValue` 讓 `FindBestMatch` 跳過。**這是上面「scorer 只排序不排除」的刻意例外**，
  理由：排除條件需要「每 tick 對每個候選 receiver 重判」，現有兩條路都做不到 ——
  `EffectHitConditionWrapper` 只在 enter 那一刻判一次（物件先進範圍再被玩家抓起會漏），receiver 端 `[If]` 又長在被抓物件
  的共用 prefab 上（加 d_IsGrabbed == true 會讓玩家抓不到沒被抓的東西）。Inspector 有 `_lastReceiver` / `_lastResult`
  enum 顯示每個候選被剔除的理由，所以「候選有東西但 best match 是 null」查得出來。首用：飛行怪 steal 的 [Dealer] Gravity Grab。
