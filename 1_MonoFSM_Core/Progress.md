# Progress

- 新增 `SetVarFloatToCurrentTimeAction` / `SinceVarFloatTimeStampCondition`：用 VarFloat 記時間戳＋算時間差做冷卻，不需要 timer 倒數。
- 新增 cheat 瞬移：`CheatTeleportPoint`（場景 marker，自動排序＋Gizmo 標號）＋ `CheatTeleportPoints`（Alt+1~9 / Alt+T，透過子節點的 `IArgEventReceiver<Vector3>` 瞬移玩家）。
- `EffectDetector` 修「enter 錯過一次就永久失效」：enter 只在 detectable 剛進重疊那一幀判，若當下 receiver 註冊鏈還沒完成（`EffectDetectable` 尚未 `AddExternalDict` 到 bindingRoot），`detectable.Get(effectType)` 拿不到 receiver 就靜默失敗，之後 detectable 一直算「持續重疊」再也不會重試。改成 stay 時若 receiver 存在但尚未 entered 就補跑一次 enter。
- DescriptionTag 區分 Ref/Getter：`AbstractValueSource<T>` 實作 `IValueSettable<T>` 時 tag 改成 `Ref`（原本一律 `=>`）；`AbstractMonoVariable` 也新增 `IsValueSourceSettable`（`TypedMonoVariable<T>` 以 `valueSource is IValueSettable<T>` override），有可寫來源時顯示 `[Ref]` 而非 `[Getter]`，與 `AbstractFieldVariable.SetValue` 的判斷一致。
