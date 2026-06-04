using _1_MonoFSM_Core.Runtime._3_FlagData.DataFunction;
using MonoFSM.Core.Variable;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Condition
{
    //檢查 entity list 裡的材料總量是否滿足 recipe 需求
    public class IsRecipeMaterialMetCondition : AbstractConditionBehaviour
    {
        [DropDownRef] [SerializeField] private VarGameData _recipeDataVar;
        [DropDownRef] [SerializeField] private VarListEntity _materialEntities;

        private RecipeDataFunction RecipeDataFunction =>
            _recipeDataVar?.Value != null
                ? _recipeDataVar.Value.GetDataFunction<RecipeDataFunction>()
                : null;

        protected override bool IsValid
        {
            get
            {
                var recipe = RecipeDataFunction;
                if (recipe == null)
                    return false;
                return recipe.IsMaterialMet(_materialEntities);
            }
        }

        public override string Description =>
            $"Recipe [{(_recipeDataVar != null ? _recipeDataVar.name : "?")}] materials met in [{(_materialEntities != null ? _materialEntities.name : "?")}]";
    }
}
