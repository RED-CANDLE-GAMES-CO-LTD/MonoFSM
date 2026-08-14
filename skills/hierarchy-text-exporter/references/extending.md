# 擴充 HierarchyTextExporter

## HierarchyExportOptions 欄位

| 欄位 | 預設 | 說明 |
|---|---|---|
| `_maxDepth` | -1 | -1 = 不限深度 |
| `_maxChildrenPerNode` | 30 | 同層子節點上限 |
| `_excludeDefaults` | true | 排除等於型別預設值的欄位 |
| `_foldKnownSubtrees` | true | 是否啟用已知子樹摺疊 |
| `_foldBareTransformChains` | true | 整棵子樹只有 Transform 時摺成一行（rig bones） |
| `_expandPaths` | [] | root 相對路徑清單；尾端 `/*` = 整棵展開；單一 `*` = 全展開 |
| `_expandDepthOverride` | -1 | 保留欄位，目前未影響展開邏輯本身之外的行為 |
| `_includeComponents` | [] | 空 = 全部；短名或 FullName，含子類（用 assignable 比對） |
| `_excludeComponents` | [] | 同上，排除用 |
| `_showOverridesOnly` | false | 只列 prefab override 欄位，忽略 `_excludeDefaults` |
| `_markOverrides` | true | override 欄位名後加 `*` |
| `_includeInactive` | true | false 時 inactive 子樹摺成一行 |
| `_maxStringLength` | 60 | string 截斷長度 |
| `_maxNoteLength` | 120 | node 行尾 note 的截斷長度（比一般 string 寬，note 是 why 的唯一出處） |
| `_maxArrayElements` | 8 | 陣列展開元素上限 |
| `_maxNestedDepth` | 2 | 巢狀 serializable 展開深度上限 |
| `_maxFieldCharsPerComponent` | 400 | 單一 component 欄位文字總量上限，超過補 `…(+N more)`；<=0 不限 |

`HierarchyExportOptions.Default` / `HierarchyExportOptions.FullExpand`（`_foldKnownSubtrees=false`、`_foldBareTransformChains=false`、`_maxChildrenPerNode=int.MaxValue`、`_maxFieldCharsPerComponent=0`）為兩個常用預設。

展開判定（`IsForcedExpand(path)`）：node 相對路徑 `path` 被視為強制展開，若任一 `_expandPaths` 項目 `e` 符合：`e == "*"`、`path == e`、`e` 以 `/*` 結尾且 `path` 落在該子樹下、或 `e` 是 `path` 的後代路徑（讓祖先自動展開以顯示後代的展開路徑）。

## 常見擴充

- **新增 summarizer**：實作 `ISubtreeSummarizer`（`Priority`/`CanSummarize`/`Summarize`），在 `SubtreeSummarizerRegistry` 靜態建構子或外部呼叫 `SubtreeSummarizerRegistry.Register(new XxxSummarizer())`
- **新增值格式化型別**：在 `CompactValueFormatter.FormatValue` 的 switch 補上新 `SerializedPropertyType` case
- **Component 篩選**：用 `HierarchyExportOptions._includeComponents` / `_excludeComponents` 傳短名或 FullName（`MatchesAny` 會用 `IsAssignableFrom` 涵蓋子類）
- **無法 AddComponent 的型別**：`ComponentDefaultCache` 每個型別用獨立子物件建預設 instance（避免 Renderer 互斥 / DisallowMultipleComponent）；`ParticleSystemRenderer` 已特例處理（先加 `ParticleSystem` 再抓）。若有其他「只能跟宿主 component 一起存在」的型別走了 heuristic 導致預設欄位全印，比照在 `GetDefault` 加特例

## 與 UnityTypeFormatter 的關係

`UnityTypeFormatter.FormatUnityObject` 原本有 FIXME：只要 `AssetDatabase.GetAssetPath` 非空就當成外部 asset，導致同一份 prefab 內的 node/component reference 被誤判成 `ExtResource`。已修正為：非 persistent → NodePath；persistent 但與目前序列化的 container 同一個 asset path → 也當 NodePath；其餘才是 ExtResource（透過內部 overload 帶 `containerAssetPath` 參數，對外的單參數簽名不變）。

`HierarchyTextExporter` / `CompactValueFormatter` 是完全獨立的新格式（`res:` / `@相對路徑` / `@/場景路徑`），不呼叫 `UnityTypeFormatter.FormatUnityObject`，但邏輯上做的是同一件事（區分樹內 reference vs 外部 asset vs 樹外 scene reference），只是輸出風格不同、且有 root-subtree 資訊可用，判斷更準確。
