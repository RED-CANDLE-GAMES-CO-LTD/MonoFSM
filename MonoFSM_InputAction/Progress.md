# Progress

- 新增 PromptDeviceFamily 機種軸、PromptIconRegistry、sprite tag 支援，以及 Editor 的 Kenney sheet 一鍵切片/建 TMP Sprite Asset 工具（InputPromptSpriteAssetBuildConfig），並修掉該工具對全新未切過貼圖首次切片會漏 sprite 的 bug。
- InputSchemeWatcher 的機種軸改為「跟著實際在用的裝置走」：插上手把不主動切（等它真的產生輸入），只有正在用的裝置被拔掉才重新解析，避免開機時手把插著卻用鍵鼠玩而顯示錯的 icon。
- 修掉 InputPromptSpriteAssetBuildConfig 的座標翻轉 bug：Kenney 這份 sheet 是左下往右上排列，xml 的 y 已經是左下原點，跟 Unity sprite rect / TMP glyphRect 同一套座標系，原本 `y = textureHeight - xmlY - h` 的翻轉是多餘的，害切片與 glyphRect 兩處都上下鏡射錯位（keyboard_e 顯示成「+」、_icon 抓到別張圖），而且兩邊都不報錯。已用像素比對（sheet 該 rect vs Default/ 的同名單張 png，avgDiff 0）確認修正。
- InputPromptUIData.GetIcon/GetSpriteTag 支援 Editor 預覽：finder 未注入時自動抓專案裡的 PromptIconRegistry，並在 Inspector 加「Editor Preview」區塊（可切 PromptDeviceFamily、看 icon / sprite tag / 目前 finder 來源）；InputSchemeWatcher.CurrentDeviceFamily 在非 Play Mode 回傳 EditorPreviewFamily。
- DeviceIconMapConfig 填表改用下拉選單＋自動補齊：_bindingPath 的選項來自專案裡所有 InputPromptUIData 實際用到的 binding path（依裝置 layout 分組、label 帶 action 名），_spriteAssetName 選 TMP Sprite Asset，另加 [Button]「從專案的 InputPromptUIData 補齊 binding path」——反查自己在 PromptIconRegistry 上掛的 family、補齊缺的路徑、並用 PromptSpriteNameSuggestion 猜好 Kenney sprite 名稱（猜不到就留空並列進 log）；已填過的欄位不覆蓋。
- InputSchemeWatcher 切手把前加 0.5 量值門檻：手把插著沒人動時的搖桿漂移/扳機殘值也會冒 ActionPerformed，原本會一直把顯示從鍵鼠搶走；現在要真的推超過門檻才切（純 button 的 EvaluateMagnitude 回 -1 則直接放行）。
- DeviceIconMapConfig：表裡只填 TMP sprite 名稱、沒填 _icon 時，Editor 下 GetIcon 會從 sprite asset 的 sprite sheet 反查同名 Sprite 供預覽，並加 [Button]「從 TMP Sprite Asset 補齊空的 _icon」把圖寫回 asset（runtime 的 icon-only 顯示才有圖）。
- 修 DeviceIconMapConfig Inspector 卡頓：[ValueDropdown] / [InfoBox] 的 resolver 會在每次 repaint、每個 entry 各跑一次 AssetDatabase.FindAssets + LoadAssetAtPath（TMP Sprite Asset 連 material/texture 一起載），改在 PromptIconMapEditorUtility 統一 cache（binding usages / dropdown / sprite asset 名稱 / sprite 名稱 / sheet 內 Sprite / owner family），projectChanged 或 30 秒 TTL 才重算；列 sprite asset 名稱改讀檔名不再 Load asset。
- _spriteAssetName 從每筆 entry 提到 DeviceIconMapConfig 層級（一份 config 只服務一個機種），舊資料用 FormerlySerializedAs 讀進 _legacySpriteAssetName 後自動搬上來並存檔；entry 靠 [NonSerialized] _owner 回頭問 config 拿 sprite asset 名稱。
- InputPromptUIData 加 [Button]「補齊各機種的 icon 對照」：只拿這一則提示的 binding path，走 PromptIconRegistry 掛的每個 family config 補 entry / sprite 名稱建議 / icon（共用 DeviceIconMapConfig.FillEntriesFor），新做一則提示時不用再去五份 config 各按一次；補完印每台裝置的統計與要人工補的路徑。
- InputPromptUIData 的 Editor Preview 加「各機種對照」表：一次列出所有 PromptDeviceFamily 的 icon 與 sprite tag（PromptIconRegistry 新增可指定 family 的 GetIcon/GetSpriteTag overload），方便看哪台裝置漏填。
