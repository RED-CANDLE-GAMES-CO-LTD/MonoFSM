# Progress

- `AbstractStateBehaviour.CanEnterState()` 原本在 result 為 true 時跑 `this.Log("Can Enter State: ", Name)`。
  `Name` 是 `gameObject.name`，Unity 的 name getter 每次呼叫都會 marshal 出一條新 string；
  `MonoExtensionLogger.Log` 只有 `[Conditional("UNITY_EDITOR")]`，參數求值在 logging 開關之外，
  所以 Editor 裡不管有沒有開 DebugProvider，每個「問我能不能進這個 state」的呼叫都吃一次 GC
  （CanEnterState 走在 JumpCheck 這種每 tick 的輸入判定路徑上）。
  改成寫進 `[ShowInInspector][ReadOnly] _lastCanEnterResult`。
  **慣例**：這條路徑上（CanEnterState / CanExitState / CanTransition / Condition.FinalResult）
  一律不准放 `this.Log` 或任何會求值 `Name` / 字串串接的參數，除錯資訊走 Inspector 欄位。
- 2026-09-29 新增 `FsmTrace`（`Core/Debug/FsmTrace.cs` + `FsmTrace.Dump.cs`）：state 切換 + Effect 命中的 static ring buffer，給互動自動測試跟手動除錯用。
  **為什麼這樣設計**：entry 是 struct、`FsmTraceEntry[4096]` 預先配好，只存 `UnityEngine.Object` reference / int / enum，
  記錄時零 GC 不組字串，名字等到 Dump 才組。掛點一律 `if (FsmTrace.Enabled) FsmTrace.RecordXxx(...)`，寫入 method 掛
  `[Conditional("UNITY_EDITOR")][Conditional("DEVELOPMENT_BUILD")]`，正式 build 整個 call site 被拿掉、buffer 也不存在。
  掛點：`StateMachine.ChangeState`（kind 由呼叫端帶：Transition / Forced / Default）、`Render()` 裡「本機沒走過 ChangeState 的
  state 變化」記 ProxyRender（用 `_traceSimStateId/_traceSimChangeTick` 分辨，這兩個不管 trace 開沒開都寫，中途打開才不會多一筆假的）、
  `GeneralEffectReceiver.OnEffectHitEnter/Exit`、`GeneralEffectDealer.CanHitReceiver` 每個 early return（HitBlocked + enum）、
  `EffectDetector` 的 dealer invalid。transition 走 `ILastTransitionRecord.GetLastTransition(tick)`，跟 `LogStateChange` 同樣用
  「tick 一樣才算」判斷。receiver / dealer invalid 時的「第一顆失敗 condition index」只在 trace 開著時才算
  （`ConditionHelper.FirstFailedIndex`，跟 `IsAllValid` 同一套跳過規則，但不動 `IsAllValid` 本身）。
  **故意不做**：不每 tick 記「transition 沒過」—— 那會把 buffer 瞬間洗掉、也違反 CanTransition 路徑不准串字串的慣例。
  要知道「為什麼卡住」改用 `FsmTrace.CaptureSnapshot(entity)` 當下拍一張（目前 state、照實際評估順序的每條 transition 含 AnyState、
  每顆 condition 的 true/false、canExit / canEnter），scenario 的 Wait 逾時才呼叫。快照另外評估了短路後面那幾顆 condition，
  所以結果比實際判定多，第一顆失敗的那顆有標記。也不記「transition 條件過了但 TryActivateState 被 CanEnter/CanExit 擋掉」，
  那種看快照的 canExit / canEnter 欄。Enabled 存在 SessionState，進 Play Mode 的 domain reload 不會掉；buffer 在
  SubsystemRegistration 清空。入口：選單 `Tools/MonoFSM/FSM Trace/*`（Enabled / Clear / Dump All / Dump Selected Entity），
  檔案在 `Library/FsmTrace/<name>.txt`，用 `up trace` 讀；WorldUpdateSimulator Inspector 的 FsmTrace foldout 看開關與筆數。
- 2026-09-29 改名：`up trace` → `up fsm-trace`（讀 FsmTrace dump）。上一條寫的 `up trace` 指的就是它。
- 2026-09-29 FsmTrace 加 `VarChange`，掛在 `AbstractFieldVariable.SetValueExecution` 的 `OnValueSet` 旁（本地、SetValueFromNetwork、LocalPredicted 都經過這裡）跟 `GenericUnityObjectVariable.SetValueInternal`。**預設只記掛 NetworkedVarTag 的 Var**：非同步的 Var 大多是每 tick 重算的 getter / timer / 中間值，全記會瞬間洗掉 4096 筆；networked 的才是「跨端狀態」，互動測試要驗的幾乎都是它。core 不 reference Fusion，用 `GetComponent("NetworkedVarTag")` 判斷，每顆 Var 只查一次、快取在 `_fsmTraceWatch`；手動白名單 `FsmTrace.WatchVar/UnwatchVar`（選它不選 Var 上的 serialized debug bool，因為不動 prefab）。值用泛型 + `UnsafeUtility.As` 塞 double / Object ref，不 boxing 不組字串。實測第一版 networked 的 `d_Muzzle Aim Target Pos`（Vector3）一秒 60 筆洗 buffer，所以沒欄位可存的 value type（Vector3 等）只有 WatchVar 手動指定才記。writer 用 SetValue 的 byWho（ToggleBoolAction 會傳自己，dump 看得到是哪顆 action 寫的）。
- 2026-09-29 FsmTrace 加 `EventSkipped`：掛在 `AbstractEventHandler.MarkSkipped`（EventHandleImplement 每個 early return 都經過），原因用 enum `FsmTraceSkipReason`。起因是引擎蓋開場被 culling 時「有 HitEnter、沒 VarChange」只能從原始碼推。**同一顆 handler 同一個原因連續擋只記第一次**、action 有跑到才重置（`_lastTracedSkip`，NonSerialized）—— OnStateUpdate 這類每 tick 叫的 handler 在 culling 期間會洗掉整個 buffer。MarkSkipped / ClearSkipReason 從 UNITY_EDITOR-only 改成也在 DEVELOPMENT_BUILD 存在，原本的 `_lastSkipReason` 字串仍只在 Editor 寫。
- 2026-09-29 EventSkipped 拿掉 `GameObjectInactive`：實測 6 筆裡 5 筆都是刻意關掉的 OnStateEnter / OnStateUpdate 節點（專案慣例「關掉 = 設 inactive」），而自動測試流程本來就先靜態讀 prefab，inactive 節點會標 `~`，不用 runtime 再記。runtime 動態 SetActive 的少數情況靠 peek。
- 2026-09-29 `AbstractRenderBehaviour.EnterSceneStart()` 改成 `virtual`（body 還是空的）：base 早就實作 `ISceneStart`，但方法不是 virtual，子類要一次性初始化（快取 view、產生 local 外觀）只能在 class 上重新宣告 `ISceneStart` 再寫 `public new void EnterSceneStart()`，靠 C# interface 重新實作才會被 MonoObj 呼叫到 —— 寫法不直覺、忘了重新宣告介面就靜默不跑。改成 override 之後 `DismantlePartsBrokenRender`、`CableCartQueueView` 都拿掉重複宣告。故意不在 base 預設呼叫 `OnEnterRender()`（原本註解的「初始化要先跑一下？」），因為 ISceneStart 時各端資料不一定 ready，第一次 render 交給 RenderLoop。
- 2026-09-29 補充：`CableCartQueueView` 後來又拿掉 `EnterSceneStart` override，改在第一次 render 才建 view。`MonoObj.HandleSceneStart` 是**反序**分派 ISceneStart，掛在 `Context/[Event] RenderLoop` 底下的 render 會比 `[VarFolder]` 裡的 Var 先跑，那時 Var 的 `Field.Init` 還沒做，`CurrentValue` 讀到 0（實測 `_builtViewCount = 0`）。render 在 EnterSceneStart 讀別顆 Var 的「值」都會踩到；只抓 reference 沒事。
