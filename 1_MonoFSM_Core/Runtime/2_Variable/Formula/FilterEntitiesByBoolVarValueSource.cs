using System.Collections.Generic;
using MonoFSM.Runtime;
using UnityEngine;

namespace MonoFSM.Core.Formula
{
    /// <summary>
    ///     把來源 entity list 依「某個 bool 變數的值」篩選成新的 list
    ///     ex: 落雷目標排除已經壞掉(d_IsBroken == true)的 entity
    /// </summary>
    public class FilterEntitiesByBoolVarValueSource : AbstractEntityBoolVarSource<List<MonoEntity>>
    {
        [Tooltip("只保留該變數等於此值的 entity")] [SerializeField]
        private bool _expectedValue;

        [Tooltip("entity 上找不到這個變數、或該變數被 disable / inactive 時是否保留")] [SerializeField]
        private bool _keepWhenVarMissing = true;

        //避免 GC：固定重用同一個 list
        private readonly List<MonoEntity> _filtered = new();

        public override string Description =>
            _boolVarTag != null
                ? $"Filter by {_boolVarTag.name} == {_expectedValue}"
                : "No BoolVarTag";

        public override string ValueInfo => Value?.Count.ToString();

        protected override string DescriptionTag => "Filter";

        public override List<MonoEntity> Value
        {
            get
            {
                _filtered.Clear();

                if (_entities == null)
                {
                    Debug.LogError(
                        "[FilterEntitiesByBoolVarValueSource] _entities 沒設，回傳空清單", this);
                    return _filtered;
                }

                if (_boolVarTag == null)
                {
                    Debug.LogError(
                        "[FilterEntitiesByBoolVarValueSource] _boolVarTag 沒設，回傳空清單", this);
                    return _filtered;
                }

                var list = GetSourceList();
                if (list == null)
                {
                    Debug.LogError(
                        $"[FilterEntitiesByBoolVarValueSource] {_entities.name} 的來源清單是 null，回傳空清單",
                        this);
                    return _filtered;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    var entity = list[i];
                    if (entity == null)
                        continue;

                    if (!TryGetBool(entity, out var boolValue))
                    {
                        //沒有這顆 var、或 var 被 disable / inactive
                        if (_keepWhenVarMissing)
                            _filtered.Add(entity);
                        continue;
                    }

                    if (boolValue == _expectedValue)
                        _filtered.Add(entity);
                }

                return _filtered;
            }
        }
    }
}
