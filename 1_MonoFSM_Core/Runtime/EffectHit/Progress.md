# EffectHit Progress

- `TriggerEnterForDealerAndDetectable` 加上 `IsEnteredReceiver` guard，避免 dealer 剛變 valid 與 detectable 剛進入同 tick 成立時 enterNode 重複觸發兩次。
