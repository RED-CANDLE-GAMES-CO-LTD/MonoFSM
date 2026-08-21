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
- `up refs` 的 inbound 每筆命中會接上引用來源的 `_note`（`# 安全區慢慢充電`）：節點名是自動命名的（`[Action] Stamina 電力 += 2`），看不出用途，只印路徑會讓人對每一筆再下鑽一次 `read` 才知道哪筆是要找的。
- note 抽取抽成共用的 `MonoFSM.Editor.NoteText`（走 cache 過的反射，不 new SerializedObject —— 摺疊行要數整棵子樹的 note，而 `PrefabTextReader.Layered` 會把同一棵樹重跑幾十次探深度），涵蓋 `_note`（`AbstractDescriptionBehaviour` / `AbstractSOConfig`）與 `Note` 的舊 `note` 欄位。三處輸出面接上：hierarchy node 行尾（`up read` / `up scene ls` / `up obj`，同時把 `_note` 從欄位堆移除，免得被 `_maxFieldCharsPerComponent` 截掉）、摺疊行的 `(+N nodes, M notes)`、FSM markdown 的 state / transition / condition / action / variable。
- `up refs --out` 的引用目標補上 note（原本只有 inbound 有，順著引用往下追一樣看不出用途）；目標是 Transform 這種本身沒 note 的 component 時退回節點層級找。
- 路徑解析新增 `\n` 逃逸（`EditResolve.SplitPath` / `Unescape` / `EscapeName`）：localized 自動命名會把含換行的譯文塞進節點名，CLI 的 op 是一行一個，不逃逸就完全指不到。hierarchy 匯出的節點名也一起逃逸（`HierarchyTextExporter.NodeName`），不然候選抄不回來。
- `set` 支援 long（`ApplyValue` 的 Integer 分支超出 int 範圍走 `longValue`）：`m_TableEntryReference.m_KeyId` 原本會 OverflowException；四處顯示端（`CompactValueFormatter` / `UnityTypeFormatter` / `PrefabToTextExporter` / `EditResolve.Preview`）與 `ComponentDefaultCache` 的預設值判斷一併改讀 `longValue`，否則 key 會被截斷成負數、或被誤判成 0 而整個欄位不輸出。
- `up prompt --check`（`PromptEdit.Check`）：只驗不改，印出每顆 value source 組出的字串＋inspector 的「Token 檢查」報告（`LocalizedStringValueSource.GetTokenReportEditor`）。手工組的 `ConditionRef` / `SmartStringTokenBinding` 是 `--case` 蓋不到的，之前只能開 Unity 用眼睛看。順手修 `PromptEdit.ResolveNode`：改用逐一比 `child.name`（`Transform.Find` 對名稱含換行的節點一律找不到）並支援 `\/` `\n`，路徑錯也會把原因印出來。
