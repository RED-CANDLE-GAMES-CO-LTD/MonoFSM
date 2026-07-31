# LevelDesign Progress

- PlayerStartSpawnPoint：新增 OriSpawnPosition / PlayTestSpawnPosition 供出生流程選位置；新增 cheat「Ctrl/Cmd + V 貼上 webhook pos 連結即時瞬移」(_enablePasteTeleportCheat，TryParsePosFromLink 自解字串不加 asmdef 依賴，同幀只處理一次)；ProcessTeleport 補 _playerTeleporter 為 null 的警告。
