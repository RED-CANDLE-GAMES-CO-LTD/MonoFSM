# 設計取捨、已知限制、模組結構

改動 uprefab 本身（`MonoFSM/Tools~/uprefab/*.py`、`PrefabTextReader` / `PrefabEdit` /
`SceneEdit`…）之前先讀這份，避免把已經驗證過的取捨改回去。

## 為什麼離線索引不能讀內容

離線 YAML 讀不到 variant 繼承來的東西。Unity 只在「本檔有東西引用到」時才寫出 stripped
佔位 document，那些節點的名稱、component、真值**全部只存在 base prefab 裡**。

實際後果：`PPlayer.prefab` 694 個索引節點裡有 259 個 `parent=0`，因為它們的 `m_Father`
指向 stripped Transform（沒有 `m_GameObject` 欄位）。多層 variant 的合成 fileID 更是
任何單一檔案裡都查不到。

所以離線索引（`find` / `overrides`）只負責「在哪個檔案」，內容一律走 Unity 匯出的結果
（`prefab read` / `scene ls`）—— 那才是**合併後**的真值。

## 為什麼沒有落檔 cache 了

原本有一套「掛 `PrefabTextCacheMarker`、存檔時寫 `.md` 到 `Tools/uprefab/cache/`」的機制，
理由是大 prefab 的匯出結果落成檔案可以先 `grep` 再只讀那 60 行，而回傳值一定整份進 context。

**2026-07-28 拆掉了。** 過期成本壓過省下的 context：實測 5 份 cache 有 2 份比來源舊
（差 80～135 秒），而照過期 cache 做的分析會給出「看起來合理但已經不成立」的結論 ——
這種錯最難察覺。加上它要靠人記得掛 marker、記得掃新舊。

`--budget` 分層拿到同樣的省 context 效果（PPlayer 122KB → 17KB），而且讀到的一定是當下真值。

## 已知限制

- **離線索引的階層在 variant 邊界會斷**（見上面）。已有 `pending_parent` 表 +
  `_resolve_stripped_parents()` 跨檔回推，但只解出 153/2414 —— 中間層常常只有 stripped
  Transform、沒有對應的 stripped GameObject，鏈就斷了。
  **不要再往這個方向投資**，要階層就用 `prefab read`，或 `find --resolve` 讓 Unity 直接算。
- **`find --resolve` 解不開的情況**：索引過期（節點已刪 / 已改名）、anchor 在沒開著的
  scene 裡、同一個 fileID 在合併後對到多個節點。都會明講原因，不會猜一條路徑給你。
- **override target 解析率約 66%**（30% 只知道來源資產、2% 完全未解析）。
- 每個 document 最多收 64 條引用邊（`MAX_REFS_PER_DOC`）。
- **`refs` 只查單一 prefab / 當前 scene 之內**，跨資產的全庫粗查還沒做。
- **離線索引（`index` / `find` / `guid` / `overrides` / `scope`）只讀不寫**。要改就要 Unity 開著
  （`prefab do` / `scene do` 都走 uloop）。
- **`up fields <TypeName>` 只吃 Component 型別**（走反射，不需要先有實例）。查
  ScriptableObject 的欄位要用 `up asset fields <assetPath>`（走 SerializedObject，需要先有
  一個實際的 asset）；巢狀 serializable class 兩邊都查不到型別定義本身，但巢狀欄位打錯時
  `set` 的錯誤訊息會列出那一層有什麼，繞得過去。
- **`scene` 系列作用在「當前開著的 active scene」**，不是路徑參數。先 `scene open` / `scene copy`。
- **Play Mode 中不能開 / 建 scene**（會直接 abort，不會半途壞掉）。

## 模組

```
MonoFSM/Tools~/uprefab/
  uyaml.py     Unity YAML streaming document scanner（不用通用 YAML parser）
  scripts.py   .cs.meta → guid/class/namespace 對照表
  config.py    .uprefab.json 讀取與路徑比對
  indexer.py   SQLite schema 與索引建置
  query.py     find / overrides / scope stats / guid ⇄ path
  unity.py     uloop 橋接：只回 Result，Domain Reload 時自己等再重試
  uprefab.py   CLI 進入點

MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/
  PrefabTextReader.cs       prefab 匯出 + charBudget 分層 + --fsm
  EditResolve.cs            路徑 / 型別 / 欄位解析與錯誤訊息（prefab / scene / asset 共用）
  EditBatch.cs              一行一操作的 DSL
  PrefabEdit.cs             prefab 寫入 + CreateVariant
  SceneEdit.cs              scene 寫入 + CopyScene + Export + Count
  AssetEdit.cs              ScriptableObject asset 建立與編輯（CreateAsset / SetField /
                            SetAssetRef / AddArrayElement / ListFields）
  EditProbe.cs              Types / Fields / Peek
  EditRefs.cs               引用反查（PrefabRefs / SceneRefs）
  EditAnchor.cs             離線 anchor（資產#fileID）→ 合併後可下鑽的路徑（find --resolve）
  AssetRef.cs               asset path → 該塞進 ObjectReference 的物件
MonoFSM-Pro/Editor/PromptEdit.cs                                       localized 文字提示
Assets/0_Gameplay/Editor/PrefabTextReaderConfig.cs                     專案設定注入
```

`EditResolve` 是刻意共用的：prefab 與 scene 只差在 root 怎麼來（prefab 有唯一 root、
scene 有多個 root object），路徑語彙與**錯誤訊息**不該有兩份 —— 錯誤訊息是修正下一步
的唯一線索。

工具本體都在 MonoFSM，**專案端只剩 `PrefabTextReaderConfig`**：把專案特有的視覺
component（FMOD `StudioEventEmitter` / FinalIK `IK` / `HighlightEffect`）加進
`PrefabTextReader.VisualComponents`。MonoFSM 那邊只放 Unity 內建的。

實際的文字格式規則（node 行、component 區塊、值格式化、摺疊摘要）見
`monofsm:hierarchy-text-exporter` skill —— 那才是格式的真相來源，這裡不重複。

開發進度與待辦見 `MonoFSM/Tools~/uprefab/PROGRESS.md`。
