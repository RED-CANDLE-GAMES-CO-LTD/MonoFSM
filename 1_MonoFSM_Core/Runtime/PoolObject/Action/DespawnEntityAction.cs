using System.Linq;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime.Variable;
using UnityEngine;

namespace MonoFSM.Runtime.ObjectPool
{
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
