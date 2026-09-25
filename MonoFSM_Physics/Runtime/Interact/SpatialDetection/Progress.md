# SpatialDetection Progress

- AbstractCastCache 新增忽略命中機制：手綁 `_ignoreEntities`（+ 可選 `_ignoreSelfEntity`），ISceneAwake 時把這些 Entity 底下的 collider 攤平成 HashSet，cast 後每幀 O(1) 查表濾掉。解決 SphereCast 從玩家身上發射會打到自己的問題（layer 表達不了「誰發射的」）。執行期用 `AddIgnoreEntity` / `RemoveIgnoreEntity` 增減。

- EffectDetector 改回 `IUpdateSimulate`（`SimulateOrder = -1000`）。搬到 BeforeSimulate phase 會讓 trigger 型 source 恆為空：`FusionSimulatorRunner` 是 `IBeforeTick`，跑在所有 `FixedUpdateNetwork`（含物理步進）之前，而 `TriggerDetectorSource` 靠 `OnTriggerStay` 餵資料，等於永遠在物理之前讀 + 清空集合。
- 2026-09-25 TriggerDetectorSource 綁死 `Detector` layer：layer 名字 / index 只定義在 core 的 `DetectorLayer`（NameToLayer 結果快取），`AbstractDetectionSource.RequiresDetectorLayer` 讓 uprefab 的檢查不用 reference Physics asmdef。Editor 下 Reset 直接改、OnValidate 延到 `EditorApplication.delayCall` 改（OnValidate 當下改 GameObject 屬性可能噴 SendMessage warning），runtime 不改 —— 資料要在 prefab 上就是對的。**故意不開 per-instance 的 serialized layer 欄位**：開了每顆 instance 都可能冒 override，還會讓人以為可以隨便挑 layer。目前全專案只有一顆 Detector layer；以後為了效能要拆，就把 `DetectorLayer` 改成可設定來源（例如 TriggerDetectorSource 上的欄位、預設仍指向 Detector），呼叫端只讀 `DetectorLayer.Index` 不用動。Cast / Overlap 不綁，它們打什麼看 query mask。
