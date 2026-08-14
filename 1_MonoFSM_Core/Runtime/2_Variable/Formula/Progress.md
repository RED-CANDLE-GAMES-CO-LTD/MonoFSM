# Formula Progress

- 新增 `FilterEntitiesByBoolVarValueSource`（entity 清單依某顆 bool var 篩選，輸出 list，內部重用 list 避免 GC），並抽出共用基底 `AbstractEntityBoolVarSource<T>`（`_entities` / `_boolVarTag` / `GetSourceList()` / `TryGetBool()`），`CountTrueOfEntitiesValueSource`、`AggregateBoolOfEntitiesValueSource` 改繼承之，各自語意不變。
