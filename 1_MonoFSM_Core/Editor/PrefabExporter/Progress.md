# PrefabExporter Progress

- 新增 `NoteText`：`_note` / `note` 的共用抽取（反射 + 型別 cache），供 `HierarchyTextExporter`、`FsmTextExporter`、`EditRefs` 共用。
- `HierarchyTextExporter`：node 行尾輸出 `# note`（`_note` / `note` 不再進欄位堆），摺疊行改走 `FoldTail` 帶 `(+N nodes, M notes)`；新增選項 `_maxNoteLength`（120）。
- `FsmTextExporter`：state / transition / condition / action / variable 都接上 note（上限 200 字）。原本 `Note` component 只會輸出 `- [FIXME]  (Note)` 這種空殼行，內容整段丟失。
- `DumpFieldValuesContextMenu`：新增 `Apply GameObject Name to Prefab (Innermost)`，把名字 override 一路 apply 到 nested prefab 鏈最內層的 asset（與既有 Revert 成對）。
- `FsmTextExporter`：掛在 action 底下的 transition（例：`AnimatorPlayAction` 底下的 `→ TimeUp Reset` + `AnimationDoneCondition`）以前被當成 action 子樹濾掉，dump 看起來像 state 沒出口，agent 因此誤判 Interact module 的 Interacted 會卡住。現在照樣列成 state 的出口，尾端標 `(掛在 <action> 底下)`。
