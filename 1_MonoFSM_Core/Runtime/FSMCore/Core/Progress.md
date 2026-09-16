# Progress
- 2026-09-11 `AbstractStateBehaviour.CanExitState` 原本只 `return true`，`_canExitNode` 宣告了但沒人讀，掛 `CanExitNode` 完全無效。改成照 `CanEnterState` 的寫法讀 `FinalResult`，加 `_lastCanExitResult` Inspector 除錯欄。呼叫鏈本來就通（transition 檢查與 `StateMachine.TryActivateState` 都會問），只補這一處。
