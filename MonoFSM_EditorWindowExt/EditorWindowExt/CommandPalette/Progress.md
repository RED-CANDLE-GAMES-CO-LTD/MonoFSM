# Progress

- Command Palette 加「Link」按鈕（Cmd/Ctrl+L）：把選取的 MenuItem 複製成可執行的 unity link，Prefab / SO / Scene 則複製成 asset_guid 連結
- 結果列右側顯示 MenuItem 快捷鍵。熱鍵**不是**從 `Menu.GetMenuItems().path` 解析得到的（Unity 在那裡已經把尾碼拿掉），要用 TypeCache 掃 `[MenuItem]` attribute 原文建 path→shortcut 表再反查；解析 path 尾端只留作 fallback
- 新增 `SearchMode.Cheats`：讀執行期的 `CheatRegistry`，只有 Play Mode 有結果，所以刻意不快取（每次搜尋重讀）。按住型 cheat（例如按住 0 加速）顯示成灰色且不可執行，它沒有一次性語意
- 加 All / Cheats 分頁（搜尋框下方一列 GUI.Toolbar，選擇存 EditorPrefs）。Cheats 分頁空 query 就列出全部 registry entries（上限 300，不像 All 分頁每組只留 5 筆），非 Play Mode 顯示提示而不是空白；registry 會隨物件 enable/disable 增減，用 entry 數量變化當 dirty 判斷重搜，不每幀重搜
- Cheats 每列右側兩顆 mini button（執行 / 跳到節點）。GUI.Button 會先吃掉滑鼠事件，不會落到列本身的選取 / 雙擊判定，也不影響 Enter；「執行」刻意不關面板（cheat 常要連按），Enter 觸發則維持原本關閉行為
- Cheats 分頁多一組 EDITOR CHEATS（MenuItem 型 cheat，Edit Mode 也能用）。判定用 `CheatMenuPathPrefixes` 白名單前綴，而不是掃 attribute 或另立 marker attribute：cheat 選單散在多個 asmdef、有些在第三方包裡改不動，前綴表是唯一「不用改別人程式碼就能圈出範圍」的做法，要增減就改那張表。副作用是 `CollectAllMenuItems` 的根選單要一併包含 RCGMaker / MonoFSM / RCGs，否則那些頂層選單根本沒被收集
