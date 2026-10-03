# Progress

## 2026-10-03 uprofile：agent 讀 Profiler 的 CLI
- 動機：每次查效能都手寫 execute-dynamic-code 撈 ProfilerDriver，token 又多又容易寫錯。改成固定指令 + 精簡表格。
- 路徑：`.claude/scripts/uprofile`（bash wrapper）→ `uloop uprofile`（uloop custom tool，`ULoop/UProfileTool.cs`）→ `UProfileReader`。
  - 為什麼走 custom tool 不走 execute-dynamic-code：不用每次 Roslyn 編譯、不跟 `up` 搶 execute-dynamic-code 的單一佔用、參數有 schema。
  - uloop 3.x 用 `TypeCache.GetTypesWithAttribute<UnityCliLoopToolAttribute>` 找 tool，放在任何 asmdef 都抓得到；CLI flag 是屬性名的 kebab-case（`MinMs` → `--min-ms`）。新 tool 第一次叫要 `uloop sync` 更新 `.uloop/tools.json`，wrapper 撞到 `UNKNOWN_COMMAND` 會自己 sync 一次再試。
  - **uloop 3.6 的 bool flag 不吃值**：`--clear true` 會回 `INVALID_ARGUMENT: Boolean option does not accept a value`，只能寫 `--clear`。
- 分兩個 assembly：`UProfileReader` 放 MonoFSM.Core.Editor（純 Unity API，沒裝 uloop 也能編）；`ULoop/` 自己一個 asmdef，`versionDefines` 偵測 `io.github.hatayama.uloopmcp` 定義 `MONOFSM_ULOOP`，再拿它當 `defineConstraints` —— 實測同一份 asmdef 的 versionDefines 可以當 define constraint 用，沒裝 uloop 的專案這個 assembly 直接不編。
- 踩到的坑（都是實測）：
  - **target = Play Mode 又沒在 play 時，`ProfilerDriver.enabled = true` 讀回來是 true，但一個 frame 都不會進 buffer**。所以 `start` 遇到這情況直接拒絕並教用法；`--editor` 會暫時把 `ProfilerDriver.profileEditor` 切 true，用 SessionState 記著，`stop` 切回去（撐得過 domain reload）。
  - Editor 失焦時 Edit Mode 約 100ms 才 tick 一次，EditorLoop self 會吃掉 90+ms —— 是節流等待不是效能問題，top / spikes 看到 EditorLoop 會附一行 note。
  - HierarchyFrameDataView 的 GC Alloc 欄是 **inclusive**（PlayerLoop 27.0K ≥ UpdateScene 25.3K ≥ …，frame 總量對得上）。GC.Alloc 本身會以子節點出現，所以 self GC = 自己的 GC − 非 GC.Alloc 子節點的 GC，GC.Alloc 節點本身不列成 marker，次數記成 parent 的 allocs/f。
  - `GetItemColumnDataAsFloat` 在 6.3 旁邊多了 AsSingle / AsDouble，這裡統一用 AsDouble。
  - main thread 一律 thread index 0；其他 thread 的 index 每個 frame 不保證一樣，所以用名稱解析一次、每個 frame 再用名稱找 index（先試上次的 index 當 hint）。thread 列舉是 `GetRawFrameDataView(frame, i)` 一路往上到 `!valid`。這台 Editor 有 500+ 條沒命名的 thread（名稱是 `#<id>`），錯誤訊息裡只算數量；`Worker 0..17` 這種編號系列折成一筆。比對優先序：完全相同 > 開頭 > 子字串（`worker` 要先對到 `Worker 0`，不是 `EnlightenWorker`）。
  - 同名 marker 遞迴巢狀時 total 只算最外層，不然 tot avg 會重複計。
  - `--file` 會蓋掉目前 buffer：錄製中拒絕；同一個檔（路徑 + mtime + frame 範圍都沒變）不重讀。
- 刻意不做：不自動切 deep profile、不自動進 Play Mode、`start` 不預設清 buffer（`--clear` 才清）；`stop` 一律存檔（預設 `Temp/uprofile/<時間>.data`），讓 buffer 被覆蓋後還能 `--file` 讀回來。
- 效能參考：360 frames 的 Play Mode buffer，top / gc 約 0.7 秒，spikes（先只讀 RawFrameDataView 的 frame 時間，再對入選 frame 建 hierarchy）不到 20ms。

## 2026-10-03 domain reload 中斷錄製、stop 後讀到別份 buffer
- 起因：別隻 agent `start --editor` 錄到一半遇到 Script Compilation，`stop` 回「本來就沒在錄」但 .data 照樣存；之後不帶 `--file` 的 `top` 讀到別份 buffer，自己沒發現。
- 實測 domain reload 對 Profiler 的影響（`beforeAssemblyReload` 和 reload 後的 `[InitializeOnLoad]` / delayCall 各拍一次）：
  - `ProfilerDriver.enabled`：True → **False**（錄製被停掉）
  - `ProfilerDriver.profileEditor`：**不變**（暫切的 Editor target 還在，所以一定要靠 SessionState 記得要切回）
  - buffer：**保留**（frames 0-181 → 0-183，reload 前後多了兩三個 frame）；reload 那一個 frame 的 EditorLoop total 會到 20 秒，看 spikes 時別誤判
  - 一次 ucompile 會觸發 2 次 `beforeAssemblyReload`，所以中斷點記第一次的 lastFrameIndex
- 做法：錄製狀態（recording 旗標、起始 frame、暫切 target、reload 次數 / 中斷 frame / 前後快照）全放 SessionState。`stop` / `status` 遇到「start 過但 profiler 是關的」就明講「錄製期間有 N 次 domain reload，錄到的只有 A-B」，target 照樣還原、照樣存檔。
- 最後存檔路徑 + frame 範圍也放 SessionState；`top`/`gc`/`spikes`/`frame` 沒帶 `--file`、而且目前 buffer 不是剛存的那份時，header 多印一行「讀的是目前 buffer…；最後存的是 <path>，要讀它加 --file」。
- 刻意不做：reload 後不自動重新開始錄（會在使用者不知道的時候吃效能），也不自動改讀最後存的檔 —— 讀哪份由呼叫的人決定。
- 前一節說 SessionState 那段原本就有（暫切 target），這次是把「錄製中」這件事本身也搬進去；`--file` 的「同一份不重讀」快取還是 static，reload 後頂多多讀一次，不影響正確性。

## 2026-10-03 `start --deep`、`--frames A-`、存檔改到 Library/
- 實測 Play Mode 中設 `ProfilerDriver.deepProfiling = true`：不會馬上 reload，旗標讀回 true，錄製照跑，但**錄到的不是 deep 資料**（InvokeFixedUpdateNetwork 底下還是只有 GC.Alloc，沒有 script 子 marker）；離開 Play Mode 時才 domain reload。這專案 Enter Play Mode Options 關了 domain reload，進 Play Mode 也不會讓它生效 → `--deep` 只准在 Edit Mode 用：開旗標 + `EditorUtility.RequestScriptReload()`，這次不開始錄，之後進 Play Mode 再 `uprofile start`。Edit Mode 開完 reload 之後錄到的資料是不是真的 deep，**還沒驗證**。
- **Play Mode 中開著 deep profile 錄製，疑似讓 Editor 閃退**：同一段時間又跑了 deep + target Editor（Play Mode 中），uloop 指令卡 137 秒沒回應，接著 Editor 被關掉（Editor.log 是正常的 OnApplicationQuit，沒有 crash report）。所以 `--deep` 禁止配 Editor target，`start --deep` 的輸出一律附警告：錄 ≤5 秒、錄完馬上 stop。
- **Unity 正常關閉時會把整個 `Temp/` 刪掉**，存在 `Temp/uprofile/` 的 .data 在 Editor 重開後全部消失（那份 deep 資料就這樣沒了）。預設存檔改到 `Library/uprofile/`（不會被清、已 gitignore）。
- deep 錄的檔名結尾帶 `-deep`，`--file` 讀這種檔時 header 印「ms 失真，只看 GC 來源」。.data 本身看不出是不是 deep，只能靠檔名。
- `--frames A-` = 從 A 到最後。
- 已知問題（沒修）：uprofile 沒有 owner 概念，Editor 只有一台，**別隻 agent 的 `stop` 會停掉你的錄製**（這次就發生了：我的 stop 切掉另一隻 agent 的 Play Mode 錄製，資料有存到但錄得比預期短）。要修可以在 start 記 owner（呼叫端傳 session id），stop 不是 owner 就拒絕。
- wrapper 碰到 `UNITY_SERVER_BUSY`（uloop 不標 Retryable）也會等 5 秒重試。
