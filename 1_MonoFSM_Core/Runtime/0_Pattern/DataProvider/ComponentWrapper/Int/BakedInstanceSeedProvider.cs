using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 場景/prefab 擺放物件用的「實例身份」IIntProvider：editor 時烤一個固定隨機 int 存進場景，
    /// 所有 client 載同一份場景 → 讀到相同 seed → deterministic，且完全不碰網路。
    /// 掛在 random Action 的子物件上，讓同一 prefab 的不同場景實例抽到不同結果。
    /// （runtime 動態 spawn 的物件請改用網路身份 provider，例如 Fusion 的 NetworkObjectId。）
    /// </summary>
    public class BakedInstanceSeedProvider : AbstractValueSource<int>, IIntProvider
    {
        [Tooltip("editor 時自動烤的固定隨機 seed，代表這個實例的身份")]
        [SerializeField] private int _bakedSeed;

        protected override string DescriptionTag => "Int";
        public override int Value => _bakedSeed;
        public int IntValue => _bakedSeed;
        public override string Description => $"BakedSeed={_bakedSeed}";

#if UNITY_EDITOR
        private void OnValidate()
        {
            // prefab asset 本體不烤，維持 0；只有丟進場景的實例各自烤自己的 override，避免同 prefab 撞號
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return;
            if (_bakedSeed == 0)
                RollSeed();
        }

        [Button("Roll Seed")]
        private void RollSeed()
        {
            // 用 GUID hash 當隨機來源（edit-time 不介意用非 deterministic 的來源，反正只烤一次存起來）
            _bakedSeed = System.Guid.NewGuid().GetHashCode();
            if (_bakedSeed == 0)
                _bakedSeed = 1; // 0 是「未烤」哨兵值，避開
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
