# Progress

- CheatManager 新增按 9 循環切換 Unity Localization 語言（Locale）。
- CheatManager 的 Cmd/Ctrl+Alt+R 沿用 soft reset，額外的「瞬移玩家回 SpawnPoint」由 PlayerStartSpawnPoint 自己攔 Alt 處理。
- WasKeyPressCheatCondition：wasPressedThisFrame 改走 CheatKeyLatch（執行期常駐 driver 在 dynamic Update poll、由 simulate tick 消費），修正 simulate 判定漏按
- CheatTeleportPoints 新增 `ICheatTeleportDispatcher` 分派點：TeleportToIndex 先問子樹上的 dispatcher（網路層實作，見 MonoFSM_Fusion/Scripts/Cheat/Progress.md），回 true 就不做本地 teleport。core 這層不依賴 Fusion，單機時 dispatcher 為 null 或回 false，走原本的 IArgEventReceiver<Vector3> 路徑
- 新增 `CheatRegistry`：執行期 cheat 熱鍵的總表（key + modifier + 描述 + 來源 + invoke）。動機是「有哪些 cheat」原本只散在各個 Update 的 if 判斷裡，人和 Command Palette 都查不到。觸發判定刻意**不**集中：各持有者 tick 語意不同（CheatManager 在 Update、CheatTeleportPoints 在 Simulate），Registry 只提供共用的 modifier 比對與 Invoke，CheatManager 也只輪詢自己登錄的那幾筆，避免別人的 entry 被重複觸發
- modifier 比對保留 required + forbidden 兩組，才還原得了原本 if/else 的互斥：Cmd+R 的 forbidden 是 Shift（所以 Cmd+Alt+R 仍走 soft reset，Alt 由 PlayerStartSpawnPoint 自己攔）、F5 的 forbidden 是 Ctrl（原本寫成 else if）
- `WasKeyPressCheatCondition` 自己登錄進 registry，Palette 觸發時走新增的 `CheatKeyLatch.Inject(Key)`：把鍵標成「已按下、還沒被 tick 消費」，讓下一個 simulate tick 的 `WasPressed` 回 true。這樣面板觸發和真按鍵共用同一條消費路徑，不用在 condition 上另開一條 cheat 專用的 IsValid 分支
- condition 走 ISceneAwake + OnEnable 雙保險登錄（idempotent）：關著的 state 子樹只有 MonoObj 的 ISceneAwake 分派得到，沒有 MonoObj 的場合才靠 OnEnable
- `CheatTeleportPoints` 的 Simulate 按鍵判定維持原樣沒有改走 registry：它有 tick 語意（一個 render frame 可能跑 0 或多個 tick），改成 Update 輪詢會漏按或重複觸發
- `CheatEntry` 加 `_owner`（UnityEngine.Object，登錄者的 gameObject）：cheat 的來源字串只夠人看、不夠點，Editor 端要能 Selection + Ping 才查得到「這顆鍵到底掛在哪個節點」。owner 放在 entry 上而不是另做一張 editor 端的對照表，是因為只有登錄的當下才知道自己是誰（同一支 prefab 會有多個實例）。runtime 只存引用、不碰 Editor API
