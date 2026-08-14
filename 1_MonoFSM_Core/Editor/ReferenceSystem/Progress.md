# ReferenceSystem Progress

- VarTagUsageScanner 支援單掃一顆 VariableTag（原本只能整顆 MonoEntityTag），VariableTag Inspector 加一鍵搜尋按鈕，結果視窗多列「用到的 Prefab」清單。
- VarTagUsageScanner 加 ScanOptions（限定搜尋資料夾、可選掃已開啟 scene / 資料夾下 scene，scene 一律走 OpenPreviewScene 唯讀掃描），結果改按「來源檔案」分組並各自附 W/R/S/O 計數，視窗清單移除 IsReadOnly 讓 Ping 按鈕可點。
