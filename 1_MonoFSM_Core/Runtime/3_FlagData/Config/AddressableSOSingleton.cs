using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MonoFSM.Core
{
    public class AddressableSOSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance;

        public static T i
        {
            get
            {
                if (instance == null)
                {
                    var op = Addressables.LoadAssetAsync<T>(typeof(T).Name);

                    instance = op.WaitForCompletion(); //Forces synchronous load so that we can return immediately
                }

                return instance;
            }
        }
    }
}