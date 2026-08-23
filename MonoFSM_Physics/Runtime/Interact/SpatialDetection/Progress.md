# SpatialDetection Progress

- AbstractCastCache 新增忽略命中機制：手綁 `_ignoreEntities`（+ 可選 `_ignoreSelfEntity`），ISceneAwake 時把這些 Entity 底下的 collider 攤平成 HashSet，cast 後每幀 O(1) 查表濾掉。解決 SphereCast 從玩家身上發射會打到自己的問題（layer 表達不了「誰發射的」）。執行期用 `AddIgnoreEntity` / `RemoveIgnoreEntity` 增減。

- EffectDetector 改回 `IUpdateSimulate`（`SimulateOrder = -1000`）。搬到 BeforeSimulate phase 會讓 trigger 型 source 恆為空：`FusionSimulatorRunner` 是 `IBeforeTick`，跑在所有 `FixedUpdateNetwork`（含物理步進）之前，而 `TriggerDetectorSource` 靠 `OnTriggerStay` 餵資料，等於永遠在物理之前讀 + 清空集合。
