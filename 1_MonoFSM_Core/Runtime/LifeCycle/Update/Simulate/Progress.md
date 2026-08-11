# Progress

- 2026-08-11: 無網路的 ManualResetLevel 改走 RequestLocalReset 排程，實際 ManualResetLevelLocal 延到 WorldUpdateSimulator.Simulate 開頭執行（tick 對齊，避免在 Update 中途重置世界）。
