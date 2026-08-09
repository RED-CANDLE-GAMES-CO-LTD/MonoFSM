# LevelDesign Progress

- PlayerStartSpawnPoint：新增 OriSpawnPosition / PlayTestSpawnPosition 供出生流程選位置；新增 cheat「Ctrl/Cmd + V 貼上 webhook pos 連結即時瞬移」(_enablePasteTeleportCheat，TryParsePosFromLink 自解字串不加 asmdef 依賴，同幀只處理一次)；ProcessTeleport 補 _playerTeleporter 為 null 的警告。
- PlayerStartSpawnPoint：新增 cheat「Ctrl/Cmd + Alt + R soft reset 關卡並把玩家瞬移回 SpawnPoint 當下位置」(_enableTeleportToSpawnCheat)，與 Cmd+R(soft reset，玩家不動) / Cmd+Shift+R(hard reset 回 oriSpawnRef) 互補。
