using System;
using System.Collections.Generic;
using MonoFSM.Core.Variable;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._3_FlagData.DataFunction
{
    [Serializable]
    public class ReceipeEntry
    {
        [SerializeField] private VariableTag _materialType;
        [SerializeField] private float _amount;

        public VariableTag MaterialType => _materialType;
        public float Amount => _amount;

        public ReceipeEntry(VariableTag materialType, float amount)
        {
            _materialType = materialType;
            _amount = amount;
        }
    }

    [Serializable]
    public class RecipeDataFunction : AbstractDataFunction
    {
        [SerializeField] ReceipeEntry[] _receipeEntries;

        //累加用，避免每次呼叫都 new dict
        private readonly Dictionary<VariableTag, float> _accumulatedAmounts = new();

        public bool IsMaterialMet(VarListEntity entityList)
        {
            if (entityList == null || _receipeEntries == null || _receipeEntries.Length == 0)
                return false;

            //先把每種需要的材料歸零
            _accumulatedAmounts.Clear();
            foreach (var entry in _receipeEntries)
            {
                if (entry.MaterialType == null)
                {
                    Debug.LogError("ReceipeEntry 的 MaterialType 未設定");
                    return false;
                }

                _accumulatedAmounts[entry.MaterialType] = 0f;
            }

            //累加每個 entity 身上對應材料的 VarFloat 數量
            foreach (var entity in entityList.Value)
            {
                if (entity == null)
                    continue;

                foreach (var entry in _receipeEntries)
                {
                    var varFloat = entity.GetVar<VarFloat>(entry.MaterialType);
                    if (varFloat == null)
                        continue;

                    _accumulatedAmounts[entry.MaterialType] += varFloat.Value;
                }
            }

            //檢查是否每種材料的量都足夠
            foreach (var entry in _receipeEntries)
            {
                if (_accumulatedAmounts[entry.MaterialType] < entry.Amount)
                    return false;
            }

            return true;
        }
    }
}
