using System.Collections.Generic;
using Sirenix.OdinInspector;

public class CurrentItemCollectionCondition : AbstractConditionComp
{
    public AbstractGameFlagCollection collection;
    //FIXME: 用dropdown選

    private IEnumerable<GameFlagDescriptable> GetCollection()
    {
        return collection.rawCollection;
    }

    [ValueDropdown("GetCollection", IsUniqueList = true)]
    public GameFlagDescriptable targetItem;


    protected override bool isValid => collection.currentItem == targetItem;
}