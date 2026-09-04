# Progress

- **VarStat 的網路覆寫（2026-09-03）**：VarStat.CurrentValue 是 Base + modifiers 現算，若 proxy 端把權威值走 `SetValueInternal` 寫進 Field，會變成「權威 FinalValue 再疊一次本地 modifier」。所以 VarStat 回 `IsNetworkValueComputed = true`，讓 `SetValueFromNetwork` 走 `_netValue` 覆寫通道，`CurrentValue` 有覆寫值就直接回。動機：Max Stamina 的 modifier 來源（搬運質量、負重…）不見得同步，client 自己算的 Max 跳動 → Stamina 百分比 UI 抖。
- **`IsNetworkPolled` 與 `HasProxySource` 分開**：VarStat 只在有人讀 CurrentValue 時才發 OnValueChanged，NetworkedVarSync 的 dirty gate 收不到，寫出端要輪詢。但不能拿 `HasProxySource` 來表達 —— 它同時控制 Inspector 隱藏 `_localField` 與 `GetCurrentValueCore` 的拒讀路徑，VarStat 回 true 會把 BaseValue 弄壞。
