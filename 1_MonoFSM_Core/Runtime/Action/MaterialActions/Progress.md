# MaterialActions Progress

- 修正 `EnableKeywordAction` 在單一 `_renderer` 模式下完全失效：`_rendererMaterials` 是 `Material[]`（Unity 可序列化型別），從 scene/prefab 反序列化時會被初始化成長度 0 的陣列而非 null，導致 `??=` lazy init 永不執行、`ApplyKeyword` 必然越界。改為把空陣列也視為未初始化，並讓 `ApplyKeyword` 回傳成功與否——只有真的套用到 material 才寫入 `_lastEnabled`，避免失敗後被快取永久擋住重試。越界警告改為只印一次。
- `RendererCollection.CachedMaterials`：renderer 尚未 ready 時 `materials` 可能回長度 0，這種結果不再寫入 cache，下次重取。
- `SetMaterialPropertyBlockAction` 加 `_restoreSharedMaterial`（VarBool）：為 true 時 `SetPropertyBlock(null, slot)` 讓 renderer 讀回 shared material，false 才覆寫。動機是取代 `EnableKeywordAction` 走 `renderer.materials` 的做法——instance 化後 Play 中調 material asset 看不到結果、Preview 按鈕在 Edit mode 還會 leak instance。MPB 不能開關 keyword，但 URP `SampleEmission` 在 `_EMISSION` 開著時輸出就是 `_EmissionMap × _EmissionColor`，所以「關發光」= `_EmissionColor` 覆寫成黑，不用碰 keyword。陷阱：restore 會把同 slot 上其他 Action 設的 MPB 值一起清掉；MPB 會讓該 renderer 退出 SRP Batcher，只適合單件物件，大量同 material 物件仍該走 material instance。
