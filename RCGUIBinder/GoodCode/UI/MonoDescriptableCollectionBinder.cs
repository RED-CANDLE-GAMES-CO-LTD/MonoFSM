using RCGMaker.Core;

namespace RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables
{
    public class MonoDescriptableCollectionBinder:MonoDict<MonoDescriptableTag,IMonoDescriptableCollection>
    {
        // public void Inject()
        // {
        //     UIProvider.BindDescriptable(Get(UIProvider.tag));
        // }


        protected override void RemoveImplement(IMonoDescriptableCollection item)
        {
            throw new System.NotImplementedException();
        }
    }
    
}