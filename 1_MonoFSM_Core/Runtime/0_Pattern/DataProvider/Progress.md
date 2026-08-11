# Progress

- 新增 `GetVarFromAncestorSource`：沿階層往上找第一個持有指定 VariableTag 的 VariableFolder 當值來源，找不到就落回 Var 自己的 local 值（提供方不需要是 MonoEntity，可掛在任意層級）。
- 修正 `GetVarFromAncestorSource` 查找對象：改成往上逐顆 **MonoEntity**、問各自的 VariableFolder（跳過自己所屬那顆 entity），不再直接找任意層的 VariableFolder。
