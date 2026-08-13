# Progress

- 2026-08-12: `Spawn` / `SpawnVisual` 加 optional `IPoolObjectPlayer player` 參數，內部統一用 `BindLastPlayer` 綁 `PoolObject.lastPlayer`（+ editor 下的 `_lastPlayerName`），呼叫點只要傳 `this`；原本各自 `GetComponent<PoolObject>().lastPlayer = this` 的 SpawnAction / SpawnVisualAction 移除手寫綁定（順手修掉 SpawnAction 那段 `pobj == null` 只 LogError 沒 return 的 NRE），PreviewDataRenderer / PreviewDataListRenderer / SpawnTableAction / LightningStrikeSpawner 補實作 `IPoolObjectPlayer` 並傳 this。目標物件沒掛 PoolObject 時印 warning 指出來源。
- 2026-08-11: 無網路的 ManualResetLevel 改走 RequestLocalReset 排程，實際 ManualResetLevelLocal 延到 WorldUpdateSimulator.Simulate 開頭執行（tick 對齊，避免在 Update 中途重置世界）。
