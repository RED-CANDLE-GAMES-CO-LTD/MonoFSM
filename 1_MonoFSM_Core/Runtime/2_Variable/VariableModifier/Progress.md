# Progress

- VariableFloatBoundModifier 新增 `_isClampCurrentValueOnBoundChanged`（預設開啟）：每幀 polling Min/Max，邊界變動（如 Max 被 modifier 調小）時把 VarFloat 當前值重新 clamp 回範圍內。
