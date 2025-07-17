using MonoFSM.Core;

namespace MonoFSM.Runtime.Mono
{
    public class MonoDescriptableCollectionBinder : MonoDict<MonoEntityTag, IMonoDescriptableCollection>
    {
        // public void Inject()
        // {
        //     UIProvider.BindDescriptable(Get(UIProvider.tag));
        // }


        protected override void AddImplement(IMonoDescriptableCollection item)
        {
            throw new System.NotImplementedException();
        }

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