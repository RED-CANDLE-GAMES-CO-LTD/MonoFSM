using System.Collections.Generic;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Formula
{
    public class CountTrueOfEntitiesValueSource : AbstractEntityBoolVarSource<float>
    {
        public enum OutputMode
        {
            Count,
            Ratio,
        }

        [SerializeField] private OutputMode _outputMode;

        //debug用，看目前哪些 entity 的 bool var 是 true
        [ShowInInspector]
        private List<MonoEntity> DebugTrueEntities
        {
            get
            {
                var result = new List<MonoEntity>();
                var list = GetSourceList();
                if (list == null)
                    return result;

                foreach (var entity in list)
                    if (TryGetBool(entity, out var isTrue) && isTrue)
                        result.Add(entity);

                return result;
            }
        }

        public override float Value
        {
            get
            {
                var list = GetSourceList();
                if (list == null)
                    return 0f;

                var total = 0;
                var trueCount = 0;
                foreach (var entity in list)
                {
                    //沒有這顆 var 的 entity 不計入 total
                    if (!TryGetBool(entity, out var isTrue))
                        continue;

                    total++;
                    if (isTrue)
                        trueCount++;
                }

                if (_outputMode == OutputMode.Ratio)
                    return total == 0 ? 0f : (float)trueCount / total;

                return trueCount;
            }
        }
    }
}
