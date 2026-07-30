# MaterialActions Progress

- 修正 `EnableKeywordAction` 在單一 `_renderer` 模式下完全失效：`_rendererMaterials` 是 `Material[]`（Unity 可序列化型別），從 scene/prefab 反序列化時會被初始化成長度 0 的陣列而非 null，導致 `??=` lazy init 永不執行、`ApplyKeyword` 必然越界。改為把空陣列也視為未初始化，並讓 `ApplyKeyword` 回傳成功與否——只有真的套用到 material 才寫入 `_lastEnabled`，避免失敗後被快取永久擋住重試。越界警告改為只印一次。
- `RendererCollection.CachedMaterials`：renderer 尚未 ready 時 `materials` 可能回長度 0，這種結果不再寫入 cache，下次重取。
