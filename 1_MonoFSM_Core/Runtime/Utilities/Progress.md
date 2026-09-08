
## GetFieldValueFromPath 對同一層 getter 呼叫兩次（2026-09-08）
非末端分支原本寫成 `var value = getter(currentObj);`（拿來做 null 檢查）之後又 `currentObj = getter(currentObj);`。
路徑末端常是「每次現算」的 property（例如 `GameData.TitleLocalized` 會組 localized 字串），
呼叫兩次等於每幀多配一份垃圾、副作用也跑兩次。改成直接沿用 `value`。
