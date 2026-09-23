using System.Linq;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime.Variable;
using UnityEngine;

namespace MonoFSM.Runtime.ObjectPool
{
    /// <summary>
    /// 回收一顆（或一整份 list 裡的）entity：呼叫 <c>entity.BindObj.Despawn()</c>。
    /// 注意回收的是 entity 所屬的 MonoObj，不是 entity 節點本身 —— entity 若是某個組合 prefab 裡的子 entity
    /// （例：路邊發電鴿和底座 的鴿子），會連同整個組合一起消失。_despawnEntityList 有填時優先走 list。
    /// </summary>
    public class DespawnEntityAction : AbstractStateAction
    {
        public VarEntity _despawnEntity;
        public VarListEntity _despawnEntityList;

        public override string Description =>
            _despawnEntityList != null
                ? $"Despawn all entities in [{_despawnEntityList.name}]"
                : $"Despawn entity [{(_despawnEntity != null ? _despawnEntity.name : "null")}]";

        protected override void OnActionExecuteImplement()
        {
            if (_despawnEntityList != null)
            {
                //先複製一份，避免 Despawn 過程中修改到原 list
                var entities = _despawnEntityList.Value.ToList();
                Debug.Log("DespawnAction: Despawning " + entities.Count + " entities", this);
                foreach (var entity in entities)
                    entity.BindObj.Despawn();
                return;
            }

            Debug.Log("DespawnAction: Despawning entity " + _despawnEntity.Value, this);
            _despawnEntity.Value.BindObj.Despawn();
        }
    }
}
