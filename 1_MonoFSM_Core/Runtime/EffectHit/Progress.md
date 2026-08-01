# EffectHit Progress

- `TriggerEnterForDealerAndDetectable` 加上 `IsEnteredReceiver` guard，避免 dealer 剛變 valid 與 detectable 剛進入同 tick 成立時 enterNode 重複觸發兩次。
- `EffectDetector` 補上 `OnDisable` / condition 失效時的 `ClearAllDetections`，修正 detector 被關掉後不再 Simulate 導致 exit 永遠不發、`HasDealerOverlap` 卡在 true 的殘留問題。
