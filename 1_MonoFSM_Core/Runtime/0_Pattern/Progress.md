# 0_Pattern Progress

- MonoDictFolder 的 external dict 只有 `Get` 系列查得到，補上 `Contains` / `ContainsKey` / `Count` / `GetKeys` / `GetValues` / `GetStringKeys` 與 indexer 的 external fallback（原本 `MonoEntity.HasReceiverType` 對外部 receiver 一律回 false）。
