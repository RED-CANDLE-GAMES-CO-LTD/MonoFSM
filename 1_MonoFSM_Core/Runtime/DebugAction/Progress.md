# Progress

- CheatManager 新增按 9 循環切換 Unity Localization 語言（Locale）。
- CheatManager 的 Cmd/Ctrl+Alt+R 沿用 soft reset，額外的「瞬移玩家回 SpawnPoint」由 PlayerStartSpawnPoint 自己攔 Alt 處理。
- WasKeyPressCheatCondition：wasPressedThisFrame 改走 CheatKeyLatch（執行期常駐 driver 在 dynamic Update poll、由 simulate tick 消費），修正 simulate 判定漏按
