# Progress

- 2026-09-23 `_excludeDefaults` 不再濾掉值是 true 的 bool。讀 read 輸出的人（尤其 agent）會把「欄位缺席」當 false，C# 初始值 `= true` 的欄位（`SetRigidbodyKinematicAction._isKinematic`）被當成預設值省略就被讀反了。故意只放行 true：false 預設照樣省略，「缺席 = false」才一律成立，也不會多出一堆 `=off` 洗版。
