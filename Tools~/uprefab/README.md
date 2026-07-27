# uprefab

讀 Unity serialized data（prefab / scene / ScriptableObject）的兩套工具。
目的：讓人跟 LLM 都能「跳著讀」Unity 資料 —— 先精準定位，再只讀需要的那一小塊，
而不是把 182MB 的主場景塞進 context。

| 層 | 做什麼 | 需要 Unity |
|---|---|---|
| **離線索引 CLI**（`MonoFSM/Tools~/uprefab/*.py`） | 定位：某個 component / 名稱在哪些檔案裡 | ❌ |
| **Prefab Text Cache**（`Tools/uprefab/cache/`） | 內容：prefab 的階層與 FSM 架構，本機產物不進 git | ❌ 讀 / ✅ 產 |

分工的原因：離線 YAML 讀不到 variant 繼承來的東西（Unity 只在本檔有引用時才寫 stripped
佔位，那些節點的真值只存在 base prefab），所以**定位走 CLI，內容一律讀 cache**。

## 快速使用

```bash
python3 MonoFSM/Tools~/uprefab/uprefab.py index          # 建立/更新索引（增量）
python3 MonoFSM/Tools~/uprefab/uprefab.py find --comp GrabSlotHolder
python3 MonoFSM/Tools~/uprefab/uprefab.py overrides PPlayer.prefab
python3 MonoFSM/Tools~/uprefab/uprefab.py scope stats
```

索引在 repo root 的 `.uprefab.db`（已 gitignore，隨時可重建）。
實測 5323 個資產全量 25 秒、增量 3 秒、查詢 0.12 秒。

Cache 則是在 prefab 上掛 `PrefabTextCacheMarker`，存檔時自動寫出；
全量重建走 menu `MonoFSM/Prefab Text Cache/重建全部`。

## 完整用法

**看 [`skills/uprefab/SKILL.md`](../../skills/uprefab/SKILL.md)** ——
決策表（什麼情況用哪個）、CLI 每個指令、marker 的所有欄位、on-demand 精讀
`ExportSubtree()`、已知限制。那份是唯一真相來源，這裡不重複。

文字格式本身（node 行、component 區塊、值格式化、摺疊摘要規則）屬於
`HierarchyTextExporter`，見 `MonoFSM/skills/hierarchy-text-exporter/SKILL.md`。

開發進度與待辦見 [PROGRESS.md](PROGRESS.md)。
