# PrefabExporter Progress

- 新增 `NoteText`：`_note` / `note` 的共用抽取（反射 + 型別 cache），供 `HierarchyTextExporter`、`FsmTextExporter`、`EditRefs` 共用。
- `HierarchyTextExporter`：node 行尾輸出 `# note`（`_note` / `note` 不再進欄位堆），摺疊行改走 `FoldTail` 帶 `(+N nodes, M notes)`；新增選項 `_maxNoteLength`（120）。
- `FsmTextExporter`：state / transition / condition / action / variable 都接上 note（上限 200 字）。原本 `Note` component 只會輸出 `- [FIXME]  (Note)` 這種空殼行，內容整段丟失。
