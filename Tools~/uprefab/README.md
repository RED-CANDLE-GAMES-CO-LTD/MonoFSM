# uprefab

讀 / 改 Unity serialized data（prefab / scene / ScriptableObject）的工具組。
目的：讓人跟 LLM 都能「跳著讀」也「精準改」Unity 資料 —— 先定位，再只讀需要的那一小塊，
再用路徑語彙下筆，而不是把 182MB 的主場景塞進 context。

| 層 | 做什麼 | 需要 Unity |
|---|---|---|
| **離線索引**（`find` / `overrides` / `scope`） | 定位：某個 component / 名稱在哪些檔案裡 | ❌ |
| **Prefab Text Cache**（`Tools/uprefab/cache/`） | 內容：大 prefab 的階層與 FSM 架構，本機產物不進 git | ❌ 讀 / ✅ 產 |
| **讀**（`prefab read` / `scene ls` / `types` / `fields` / `peek`） | 合併後的真值、型別欄位、runtime 值 | ✅ |
| **寫**（`prefab do` / `scene do` / `prefab variant` / `scene copy`） | 用節點路徑改結構、建 variant、複製場景模板 | ✅ |
| **驗證**（`scene count` / `logs` / `play`） | Play Mode 下數物件、看錯誤 | ✅ |

離線索引與內容分工的原因：離線 YAML 讀不到 variant 繼承來的東西（Unity 只在本檔有引用時
才寫 stripped 佔位，那些節點的真值只存在 base prefab），所以**定位可以離線，內容一律回 Unity 撈**。

## 快速使用

```bash
up() { python3 "MonoFSM/Tools~/uprefab/uprefab.py" "$@"; }

up index                        # 建立/更新索引（增量）
up find --comp GrabSlotHolder
up overrides PPlayer.prefab
up scope stats

up prefab read "Assets/…/X.prefab" --node "[StateFolder] StateFolder"
up scene do "add||Spawner|MonoEntity,MonoObj" "auto|Spawner" "save"
up scene count --name 測試資源 --sample 3
```

（zsh 不會對 `$VAR` 斷詞，所以用 shell function 而不是 `UP="python3 …"`。）

索引在 repo root 的 `.uprefab.db`（已 gitignore，隨時可重建）。
實測 5323 個資產全量 25 秒、增量 3 秒、查詢 0.12 秒。

Cache 則是在 prefab 上掛 `PrefabTextCacheMarker`，存檔時自動寫出；
全量重建走 menu `MonoFSM/Prefab Text Cache/重建全部`。

## 完整用法

**看 [`skills/uprefab/SKILL.md`](../../skills/uprefab/SKILL.md)** ——
決策表（什麼情況用哪個）、每個指令、批次 DSL 的所有操作、marker 欄位、
「從零組一個 FSM 並驗證」的完整實例、已知限制。那份是唯一真相來源，這裡不重複。

文字格式本身（node 行、component 區塊、值格式化、摺疊摘要規則）屬於
`HierarchyTextExporter`，見 `MonoFSM/skills/hierarchy-text-exporter/SKILL.md`。

開發進度與待辦見 [PROGRESS.md](PROGRESS.md)。
