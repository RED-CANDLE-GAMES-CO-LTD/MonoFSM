# Progress

- 新增 `GetVarFromAncestorSource`：沿階層往上找第一個持有指定 VariableTag 的 VariableFolder 當值來源，找不到就落回 Var 自己的 local 值（提供方不需要是 MonoEntity，可掛在任意層級）。
- 修正 `GetVarFromAncestorSource` 查找對象：改成往上逐顆 **MonoEntity**、問各自的 VariableFolder（跳過自己所屬那顆 entity），不再直接找任意層的 VariableFolder。
- 2026-09-30 `GetVarFromParentEntitySource` 預設來源改成「自動跳過自己」：最近的 entity 上同 `_varTag` 的 var 就是掛著這顆 source 的 Var 時，往上找 ancestor MonoEntity，直到找到同 tag 但不是自己的那顆（結果快取）。起因：攻擊 module（落雷 / 漏電）本身是 MonoEntity，module 的 `Attack Enabled` 想讀 host 神像的 `Attack Enabled`，`[AutoParent]` 只會抓到 module 自己 → 自我參照。故意不開「往上跳幾層」的欄位：自我參照永遠是 bug，自動跳過就是唯一合理的行為，不用設定。都找不到時還是回最近的 entity，讓原本的自我參照 error 照樣噴出來。
