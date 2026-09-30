# Progress

- `AbstractEventHandler` 加 `protected virtual bool IgnoreCulling`，`OnResetStartHandler` 覆寫成 true：開場 WorldInit 時玩家還沒生成，`CullingTargetGameObjects.Awake` 已把遠方物件的 handle 關掉，原本的 culling gate 會讓所有有 culling 的物件開場那發 OnResetStart 永遠跑不到（發現點：供電插座 d_HasPower 要從神像 active 狀態初始化）。只跳過 culling，inactive / condition / authority gate 照舊；action 掛在 Simulation Culling Active Handle 底下的一樣不會跑，初始化 action 要放 handle 外。故意不做成 serialized 開關，因為「初始化事件不該被 culling 擋」是事件語意，不是個別設定。
