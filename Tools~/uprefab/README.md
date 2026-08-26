# uprefab

讀 / 改 Unity serialized data（prefab / scene / ScriptableObject）的工具組。
目的：讓人跟 LLM 都能「跳著讀」也「精準改」Unity 資料 —— 先定位，再只讀需要的那一小塊，
再用路徑語彙下筆，而不是把 182MB 的主場景塞進 context。

| 層 | 做什麼 | 需要 Unity |
|---|---|---|
| **離線索引**（`find` / `overrides` / `scope`） | 定位：某個 component / 名稱在哪些檔案裡 | ❌ |
| **讀**（`prefab read` / `scene ls` / `obj` / `types` / `fields` / `peek` / `refs`） | 合併後的真值、型別欄位、runtime 值、引用反查 | ✅ |
| **寫**（`prefab do` / `scene do` / `prefab variant` / `scene copy`） | 用節點路徑改結構、建 variant、複製場景模板 | ✅ |
| **asset**（`asset create` / `asset set` / `asset set-ref` / `asset add-element` / `asset fields`） | 建立/編輯獨立的 ScriptableObject asset（registry / config 類資料） | ✅ |
| **prompt**（`prompt`） | 幫 VarString 掛一組有條件的 localized 文字提示（含 Localization 條目、token、Auto 綁定、回傳自帶驗證） | ✅ |
| **驗證**（`scene count` / `logs` / `play`） | Play Mode 下數物件、看錯誤 | ✅ |
| **診斷**（`effect-trace`） | EffectReceiver 沒觸發：一次攤開 detector → dealer → receiver → enterNode gate | ✅ |

離線索引與內容分工的原因：離線 YAML 讀不到 variant 繼承來的東西（Unity 只在本檔有引用時
才寫 stripped 佔位，那些節點的真值只存在 base prefab），所以**定位可以離線，內容一律回 Unity 撈**。

## 快速使用

```bash
up() { python3 "MonoFSM/Tools~/uprefab/uprefab.py" "$@"; }

up index                        # 建立/更新索引（增量）
up find --comp GrabSlotHolder                       # 預設只查 full tier
up find --comp GrabSlotHolder --scope all           # 明確包含 shallow
up find --comp SetVarBoolAction --by-asset      # 命中幾千筆時先看分佈
up overrides PPlayer.prefab
up overrides 主場景.unity --by-target            # override 集中在哪個 instance / 節點
up scope stats

up prefab read "Assets/…/X.prefab" --node "[StateFolder] StateFolder"
up prefab read "Assets/…/X.prefab" --fsm-only --budget 8000
up prefab locate "Assets/…/X.prefab" --comp TransitionBehaviour --members _target,_conditions
up prefab peek "Assets/…/X.prefab" --node "…/[Transition] => spawn" \
    --comp TransitionBehaviour --members _target,_conditions   # 只要幾個欄位，別讀整棵
up obj "[名稱](http://localhost:8888/webhook?globalId=GlobalObjectId_V1-2-…)"   # 貼 scene 物件連結
up obj "<連結>" --locate                                    # 只要節點路徑 + component 清單
up refs "Assets/…/X.prefab" --node "…/[Var] Durability"    # 誰指向它（--out = 它指向誰）
up prefab peek-batch "Assets/…/X.prefab" -f probes.txt       # node|comp|members，一次 load
up scene do "add||Spawner|MonoEntity,MonoObj" "auto|Spawner" "save"
up prefab do "Assets/…/FSM.prefab" --quiet "state|[StateFolder] StateFolder|spawn" \
    "act|$|enter|Spawn|SpawnAction"          # FSM 複合操作 + `$` 代換上一個節點
up scene count --name 測試資源 --sample 3
up effect-trace "Zone Arrive Trigger 找到火車 Variant"      # receiver 沒觸發，卡在哪一段（Play Mode）

up asset create PromptIconRegistry "Assets/…/測試 Registry.asset"   # 建 ScriptableObject asset
up asset add-element "Assets/…/測試 Registry.asset" _entries        # 陣列尾端加一筆
up asset set-ref "Assets/…/測試 Registry.asset" \
    "_entries.Array.data[0]._config" "Assets/…/測試 Config.asset"    # 指向另一個 asset

up prompt "Assets/…/X.prefab" --var "…/[Getter] d_ Select Text Prompt 文字提示" \
    --case "broken|壞掉了請維修|if:…/[Getter] d_IsBroken=true" \
    --case "socket_to_charge|{key} 充電|prompt:key=RMB"             # 加 localized 文字提示
```

（zsh 不會對 `$VAR` 斷詞，所以用 shell function 而不是 `UP="python3 …"`。）

索引在 repo root 的 `.uprefab.db`（已 gitignore，隨時可重建）。
實測 5323 個資產全量 25 秒、增量 3 秒、查詢 0.12 秒。

寫入的批次 DSL 支援 `$` / `$label` 路徑代換與 `state` / `trans` / `if` / `act` 複合操作 ——
一份 FSM 從純原語的 1.5KB 降到 0.8KB，差的全是重複的長路徑。

`prefab read` / `scene ls` / `obj` 的 `--budget`（預設 20000 字元）是 hierarchy + FSM
總輸出的 hard cap；明確 `--depth` 也不能繞過，只有 `--budget 0` 才不限。用 `--node` 下鑽、
`--fsm-only` 只看狀態機、`--structure-only` 只導航。read cache 預設完全關閉，確定資產已存且
會重複讀才加 `--cache`。
實測 PPlayer 全展開 122KB → 預設 17KB。

## 完整用法

**看 [`skills/uprefab/SKILL.md`](../../skills/uprefab/SKILL.md)** ——
決策表（什麼情況用哪個）、每個指令、批次 DSL 的所有操作、
`up asset`（建立/編輯 ScriptableObject asset：`create` / `set` / `set-ref` /
`add-element` / `fields`，含「建 registry SO → 加陣列元素 → 指向另一個 SO」的完整範例）、
「從零組一個 FSM 並驗證」的完整實例、已知限制。那份是唯一真相來源，這裡不重複。

文字格式本身（node 行、component 區塊、值格式化、摺疊摘要規則）屬於
`HierarchyTextExporter`，見 `MonoFSM/skills/hierarchy-text-exporter/SKILL.md`。

開發進度與待辦見 [PROGRESS.md](PROGRESS.md)。
