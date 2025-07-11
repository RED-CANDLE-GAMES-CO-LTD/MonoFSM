using System.Collections.Generic;
using Sirenix.OdinInspector;

public class CurrentItemCollectionCondition : AbstractConditionBehaviour
{
    public AbstractGameFlagCollection collection;
    //FIXME: 用dropdown選

    private IEnumerable<DescriptableData> GetCollection()
    {
        return collection.rawCollection;
    }

    [ValueDropdown("GetCollection", IsUniqueList = true)]
    public DescriptableData targetItem;


    protected override bool IsValid => collection.currentItem == targetItem;
}