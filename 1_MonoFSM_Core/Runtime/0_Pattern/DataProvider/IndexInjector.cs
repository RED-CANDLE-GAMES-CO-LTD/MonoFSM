using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    public interface IIndexInjector
    {
        public int Index { get; }
    }

    public class IndexInjector : MonoBehaviour, IIndexInjector
    {
        public int index;
        public int Index => index;
    }
}