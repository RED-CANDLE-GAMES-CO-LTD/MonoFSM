# Progress

- CheatManager 新增按 9 循環切換 Unity Localization 語言（Locale）。
- CheatManager 的 Cmd/Ctrl+Alt+R 沿用 soft reset，額外的「瞬移玩家回 SpawnPoint」由 PlayerStartSpawnPoint 自己攔 Alt 處理。
- WasKeyPressCheatCondition：wasPressedThisFrame 改走 CheatKeyLatch（執行期常駐 driver 在 dynamic Update poll、由 simulate tick 消費），修正 simulate 判定漏按
- CheatTeleportPoints 新增 `ICheatTeleportDispatcher` 分派點：TeleportToIndex 先問子樹上的 dispatcher（網路層實作，見 MonoFSM_Fusion/Scripts/Cheat/Progress.md），回 true 就不做本地 teleport。core 這層不依賴 Fusion，單機時 dispatcher 為 null 或回 false，走原本的 IArgEventReceiver<Vector3> 路徑
