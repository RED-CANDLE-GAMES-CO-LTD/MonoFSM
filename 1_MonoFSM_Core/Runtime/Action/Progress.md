- SetVarBoolAction 新增可選 `_sourceVar`（VarBool dropdown），有指定就取它的值、蓋過常數 `TargetValue`（舊名保留以免 prefab override 失效）。
- HeadLookAtAnimatorApplier 改繼承 AbstractRenderBehaviour（原本是 AbstractStateAction，掛在 OnStateUpdate 下，client 因 authority gate 不執行導致頭骨不轉）；掛在 `[State]` 節點直接底下即可，該狀態 active 時每 render frame 觸發、兩端都跑。
- 新增 ParticlePulseRender（ParticleSystemActions）：render action，被觸發時把特效 pulse 一下（localScale 倍率 / PS startSize 倍率 / noise strength 加量）再依 `_falloffPower` 衰減回原樣；「何時閃」交給 handler（ex: OnValueDirectionChangedHandler 設 Increase），arg（變化量）配 `_fullPulseDelta` 縮放幅度。衰減靠 IRenderUpdate 每幀跑，client 端靠 Var 自己的網路同步觸發 handler，不需要 render sync。
- 新增 OnValueDirectionChangedHandler（4_Event）+ VarFloat 的 `_directionChangedHandlers`（`[AutoChildren]` 陣列）：由 `OnValueSet(old,new)` 觸發，用 enum `_direction` 分 Increase / Decrease，arg 是變化量絕對值。和 `_valueChangedHandler`（Field listener 驅動、任何變化都跑、判不出方向）互補，同一顆 Var 下兩個方向可各掛一顆。

## 2026-09-10 Render 路徑繞過 condition：InvokeEffectHitEventHandlerAction
- 症狀：ManualEvent 的 `[If]` 不成立時，`[Action]` 正確被擋，但底下的 `[Render] SpawnVisual` 照樣播。
- 成因：`InvokeEffectHitEventHandlerAction` 同時是 `IArgEventReceiver` 和 `IRenderBehaiour`。
  Simulate 走 `EventHandle()` → `EventHandleImplement` 會過 `_conditionFolder`；
  Render 由 render tier 直接呼 `OnEnterRender()` → `target.EnterRenderInvoke()`，完全繞過那道 gate。
- 修法：`AbstractEventHandler` 開 public `IsConditionValid`，轉發者在 render 路徑自己補檢查
  （自己的 `IsValid` + target 的 `IsConditionValid`），失敗理由記在 `_lastRenderSkipReason`。
  刻意不把檢查放進 `EnterRenderInvoke()` 內部：那條路同時是 proxy 端 render sync 的回放入口，
  回放當下網路變數可能已被 authority 改掉（例：`d_IsCharged` 已設回 False），會讓 proxy 端特效整組消失。
- 通則（尚未修）：`AbstractStateAction` 的子類別自行實作 `IRenderBehaiour.OnEnterRender()` 時，
  沒有任何機制強制它過 `IsValid`。`AbstractRenderBehaviour` 是把 gate 做在 base 的
  `OnEnterRender()` 裡再呼 abstract `OnEnterRenderImplement()`，所以不會漏；
  直接實作 interface 的 action（SwitchAction / SwitchCase / SetGameObjectActiveByIndexAction /
  AnimatorPlayAction / AnimationClipPlayAction）目前都漏檢查。
