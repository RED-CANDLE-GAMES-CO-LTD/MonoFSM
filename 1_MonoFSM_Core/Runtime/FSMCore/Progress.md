# Progress

- `AbstractStateBehaviour.CanEnterState()` 原本在 result 為 true 時跑 `this.Log("Can Enter State: ", Name)`。
  `Name` 是 `gameObject.name`，Unity 的 name getter 每次呼叫都會 marshal 出一條新 string；
  `MonoExtensionLogger.Log` 只有 `[Conditional("UNITY_EDITOR")]`，參數求值在 logging 開關之外，
  所以 Editor 裡不管有沒有開 DebugProvider，每個「問我能不能進這個 state」的呼叫都吃一次 GC
  （CanEnterState 走在 JumpCheck 這種每 tick 的輸入判定路徑上）。
  改成寫進 `[ShowInInspector][ReadOnly] _lastCanEnterResult`。
  **慣例**：這條路徑上（CanEnterState / CanExitState / CanTransition / Condition.FinalResult）
  一律不准放 `this.Log` 或任何會求值 `Name` / 字串串接的參數，除錯資訊走 Inspector 欄位。
