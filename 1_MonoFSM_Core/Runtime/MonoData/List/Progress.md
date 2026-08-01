# Progress

- `GetElementOfVarListSource` / `VarListCurrentIndexEqualsCondition` 加 `[AutoParent]` fallback 欄位 `_parentVarList`（`_varList` 沒指定時往上抓祖先的 VarList），讓多個 UI slot 共用同一份清單，不用每個 slot 各複製一顆 list getter。
