using UnityEngine;

namespace RCGMaker.Core.DataProvider
{
    public class GameDataProviderComp : MonoBehaviour
    {
        [SerializeReference] public IGameDataProvider provider;
    }
}