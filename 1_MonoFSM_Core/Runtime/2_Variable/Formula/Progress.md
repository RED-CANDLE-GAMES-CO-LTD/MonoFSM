# Formula Progress

- 新增 `FilterEntitiesByBoolVarValueSource`（entity 清單依某顆 bool var 篩選，輸出 list，內部重用 list 避免 GC），並抽出共用基底 `AbstractEntityBoolVarSource<T>`（`_entities` / `_boolVarTag` / `GetSourceList()` / `TryGetBool()`），`CountTrueOfEntitiesValueSource`、`AggregateBoolOfEntitiesValueSource` 改繼承之，各自語意不變。
- `TryGetBool()` 加上 disable / inactive 檢查：var component 被 disable 或所在物件 inactive 時會殘留上次的值，現在一律視為「拿不到」（不計入 Count/Ratio 的 total），並補上 `CountTrueOfEntitiesValueSource` 的 `Description` / `ValueInfo` / `DescriptionTag`。
