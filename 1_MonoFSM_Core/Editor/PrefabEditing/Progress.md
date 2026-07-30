# Progress

- 新增 AssetEdit：ScriptableObject asset 的建立/編輯 API（與 PrefabEdit/SceneEdit 同一套 batch DSL 風格）。
- 新增 EditGid：GlobalObjectId 連結（Editor 產的 scene 物件連結）→ 定位物件並匯出子樹；`PrefabTextReader.ExportNode` 抽出來供其重用。
- 路徑解析支援同名節點索引 `名稱[n]`（read 與 do 共用 `EditResolve.TryNode`），錯誤訊息列出的子節點也會替同名的標上 `[n]`。
- 新增 EditAnchor：離線索引 anchor（`資產#fileID`）→ 合併後可直接餵給 `--node` 的完整路徑（`up find --resolve`），路徑生成抽成 `EditResolve.PathOf`，scene 的 root 段也支援 `[n]`。
- 新增 batch DSL `addel|<node>|<comp>|<field>`：陣列/List 尾端加元素（`set` 改不了 `.Array.size`，ArraySize propertyType 走不進 ApplyValue），prefab / scene 兩邊共用 `EditResolve.AddArrayElement`。
