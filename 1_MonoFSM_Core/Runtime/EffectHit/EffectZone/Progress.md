- `EffectZone` 加 `ZoneCoverage`（Radius / Hierarchy / Both）：Hierarchy 模式改用「祖先身上有沒有掛 zone」判定、不進 registry；新增 `IsParentEntityHasEffectZoneCondition`，分 `ZoneLookupMode.Dynamic`（逐層往上走 transform，跟得上 runtime reparent）／`Static`（`[AutoParent]` 編輯時 cache 整條祖先鏈）／`EntityChain`（走 `MonoEntity.ParentEntity` 鏈，讀 `MonoEntity` 新加的 `[Auto] OwnEffectZone` cache）。
- `EffectZone` 補 `public float Radius`（原本只有 `RadiusSqr`），給外部拿世界半徑做視覺對齊用（ex: FogVoidSphereFitter 反推 FogVoid scale）。
- `EffectZoneEntitySource` 加 `_pickNearest`：多個同型 zone 同時罩住時取圓心最近的那顆 owner（探測器指針指向最近訊號源用）。刻意不做成獨立 component，因為「第一個命中」與「最近命中」差別只在取捨規則，語意同一件事；預設仍是第一個命中，舊接線行為不變。順手加 `[ShowInInspector]` 的覆蓋 zone 數與取到的距離，用來分辨「沒 zone」與「zone 沒 active」。

- `EffectZoneRegistry.FindCovering(type,pos,pickNearest,out count,out sqrDist)` 是唯一的「誰罩住我」掃法，EffectZoneEntitySource 與 EffectZoneSignalStrengthSource 都走它；新增 zone 查詢類 getter 時不要再自己寫 for 迴圈，避免兩邊判定漂移。訊號強度用 `1 - dist/Radius` 而不是另開欄位，讓半徑仍是 zone 唯一的距離來源。
