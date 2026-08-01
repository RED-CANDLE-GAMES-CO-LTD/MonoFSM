# Progress

- 新增 AssetEdit：ScriptableObject asset 的建立/編輯 API（與 PrefabEdit/SceneEdit 同一套 batch DSL 風格）。
- 新增 EditGid：GlobalObjectId 連結（Editor 產的 scene 物件連結）→ 定位物件並匯出子樹；`PrefabTextReader.ExportNode` 抽出來供其重用。
- 路徑解析支援同名節點索引 `名稱[n]`（read 與 do 共用 `EditResolve.TryNode`），錯誤訊息列出的子節點也會替同名的標上 `[n]`。
- 新增 EditAnchor：離線索引 anchor（`資產#fileID`）→ 合併後可直接餵給 `--node` 的完整路徑（`up find --resolve`），路徑生成抽成 `EditResolve.PathOf`，scene 的 root 段也支援 `[n]`。
- 新增 batch DSL `addel|<node>|<comp>|<field>`：陣列/List 尾端加元素（`set` 改不了 `.Array.size`，ArraySize propertyType 走不進 ApplyValue），prefab / scene 兩邊共用 `EditResolve.AddArrayElement`。
- `ApplyValue` 支援 LayerMask 欄位（整數位元遮罩 / Everything / Nothing / 逗號分隔 layer 名稱）。
- prefab batch DSL 新增 `prefab|<prefabPath>|<parent>|<name>`：在 prefab asset 內放 nested prefab 實例（把模組 prefab 裝進宿主 prefab 用），語意與 scene 版一致。
- prefab batch DSL 的 `comp` / `set` / `ref` / `aref` / `addel` 的 `<node>` 留空 = root（`MonoEntity` / `MonoObj` 都掛在 root，之前只有 `add` / `delcomp` 允許），訊息一律走 `EditResolve.Describe`。
- 新增 `idx|<node>|<siblingIndex>`（prefab / scene 共用語意，負數從尾端算）：child 順序在 MonoFSM 裡就是 value source / condition 的優先序，之前沒有調整順序的手段。
- 路徑支援 `\/` 逃逸：節點名本身含 `/`（`=> Localized: GameplayUI/grab` 這類自動命名）時 `Transform.Find` 會誤判成階層，改走自掃子節點；`ChildLabels` / `PathOf` 列出的路徑也會自動逃逸。
- 新增 `up prefab copy --out <path> [--name]`（`PrefabEdit.CopyAsset`）：複製成獨立 prefab 並順手改 root 名稱，拿既有 prefab 當模板改比從零建安全（`variant` 是要保留繼承時才用）。
- prefab batch DSL 新增 `rename|<node>|<newName>`（`<node>` 留空 = root）：複製模板後 root / 節點名字還是舊的，之前沒有改名手段。注意帶 `AbstractDescriptionBehaviour` 的節點存檔後會被自動命名蓋掉。
