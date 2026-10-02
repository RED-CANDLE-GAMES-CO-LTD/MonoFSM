# Progress

- 2026-10-02 `SpawnAction._spawnedEntityVar` 改填 `newObj.Entity`（MonoObj 的 `[AutoChildren]` entity），不再用 root `GetComponent<MonoEntity>()`：焦炭鴿、滅火器這類道具 root 只有 MonoObj / NetworkObject / PoolObject，entity 在子節點的 `[Network] 3D Culling Physics Obj Variant` 上，舊寫法拿到 null，掛貨滑索「spawn 完 mount 到托盤」整條靜默失效（沒 mount、固定 flag 寫不進去、拔河條件看 null）。root 本身有 entity 的道具結果不變。故意不用 `GetComponentInChildren`：MonoObj 已經有快取好的 Entity，不用每次 spawn 再掃。
