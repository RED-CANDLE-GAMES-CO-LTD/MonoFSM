# Progress

- 新增 `GetVarFromAncestorSource`：沿階層往上找第一個持有指定 VariableTag 的 VariableFolder 當值來源，找不到就落回 Var 自己的 local 值（提供方不需要是 MonoEntity，可掛在任意層級）。
