using RCGMaker.Core;

namespace RCGMaker.Runtime.Mono
{
    public class MonoDescriptableCollectionBinder : MonoDict<MonoDescriptableTag, IMonoDescriptableCollection>
    {
        // public void Inject()
        // {
        //     UIProvider.BindDescriptable(Get(UIProvider.tag));
        // }


        protected override void RemoveImplement(IMonoDescriptableCollection item)
        {
            // throw new System.NotImplementedException();
        }

        protected override bool CanBeAdded(IMonoDescriptableCollection item)
        {
            return item.isActiveAndEnabled;
        }
    }
}