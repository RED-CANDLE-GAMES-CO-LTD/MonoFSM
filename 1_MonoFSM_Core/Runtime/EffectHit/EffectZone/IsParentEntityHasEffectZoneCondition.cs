using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.EffectZone
{
    /// <summary>怎麼取得「我頭上那些候選 zone」。</summary>
    public enum ZoneLookupMode
    {
        [Tooltip("每次求值逐層往上走 transform.parent，支援 runtime reparent（ex: 東西被搬上／搬下車廂）")]
        Dynamic = 0,

        [Tooltip("編輯時就 cache 好整條祖先鏈的 zone，零 GetComponent 成本，但從屬關係固定不變")]
        Static = 1,

        [Tooltip(
            "沿 MonoEntity.ParentEntity 鏈往上，讀每顆 entity 自己 cache 的 OwnEffectZone，零 GetComponent；zone 必須掛在 entity 的 root 物件上")]
        EntityChain = 2,
    }

    /// <summary>
    /// 「我的某個祖先身上有沒有掛運作中的 EffectZone」——用 Hierarchy 從屬關係取代距離判定。
    /// ex: 供電的 zone 掛在車廂 root，車廂底下任意深度的東西都算有電；車上的東西被搬下車就自動失效。
    ///
    /// 和 IsInEffectZoneCondition 的差別：那支掃 registry 比距離（適合「附近」語意），
    /// 這支看 Hierarchy 從屬（適合「屬於誰」語意），不需要位置、也不進 registry。
    /// 同樣是純 pull 無狀態，level reset 免疫。
    /// </summary>
    public class IsParentEntityHasEffectZoneCondition : AbstractConditionBehaviour
    {
        [Required]
        [SOConfig("GeneralEffectType")]
        [Tooltip("要找哪一種區域（ex: d_PowerZone 供電區）")]
        [SerializeField]
        private GeneralEffectType _zoneType;

        [Tooltip(
            "Dynamic: 每次求值往上走 transform，跟得上 runtime reparent\n"
            + "Static: 用編輯時 cache 的祖先鏈，零成本但從屬固定\n"
            + "EntityChain: 走 entity 階層 + entity 自己的 cache，零成本；zone 要掛在 entity root 上")]
        [SerializeField]
        private ZoneLookupMode _lookup = ZoneLookupMode.Dynamic;

        [ShowIf("@_lookup == ZoneLookupMode.Dynamic")]
        [Tooltip("從哪裡開始往上找，留空則用自己的 transform（會含自己身上的 zone）")]
        [SerializeField]
        private Transform _searchRootOverride;

        //Static 模式用：編輯時把整條祖先鏈的 zone 都填進來（含自己身上的）。
        //cache 整條而不是只 cache 最近一顆——最近那顆的 zoneType 未必是我要找的那種。
        [ShowIf("@_lookup == ZoneLookupMode.Static")]
        [AutoParent(getMadIfMissing: false)]
        [SerializeField]
        private EffectZone[] _cachedZones;

        private Transform SearchRoot =>
            _searchRootOverride != null ? _searchRootOverride : transform;

        //命中的那個 zone，除錯用（Inspector 看得到現在是掛在哪一層供電）
        [ShowInInspector] private EffectZone _lastCoveringZone;

        protected override bool IsValid
        {
            get
            {
                if (_zoneType == null)
                    return false;

                var found = _lookup switch
                {
                    ZoneLookupMode.Static => FindInCachedZones(),
                    ZoneLookupMode.EntityChain => FindByEntityChain(),
                    _ => FindByWalkingUp(),
                };

                _lastCoveringZone = found;
                return found != null;
            }
        }

        //逐層 GetComponent<T>（單一泛型版無 alloc），而不是 GetComponentInParent：
        //後者只回第一顆命中的 EffectZone，zoneType 不對就會漏掉更上層的；
        //GetComponentsInParent 又會配陣列（GC）。
        private EffectZone FindByWalkingUp()
        {
            for (var t = SearchRoot; t != null; t = t.parent)
            {
                //EffectZone 是 DisallowMultipleComponent，一層最多一顆
                var zone = t.GetComponent<EffectZone>();
                if (IsMatch(zone))
                    return zone;
            }

            return null;
        }

        //走 entity 階層而不是 transform 階層：中間那些純視覺／結構用的空物件會被跳過，
        //每層只讀 entity 編輯時 cache 好的 OwnEffectZone，沒有 GetComponent。
        //代價：zone 必須掛在 entity 的 root 物件上（掛在 entity 底下的子物件會抓不到），
        //且 MonoEntity._parentEntity 是 Awake 快照，runtime reparent 不會更新——要跟得上就用 Dynamic。
        private EffectZone FindByEntityChain()
        {
            for (var e = BindEntity; e != null; e = e.ParentEntity)
            {
                var zone = e.GetCompCache<EffectZone>();
                if (IsMatch(zone))
                    return zone;
            }

            return null;
        }

        private EffectZone FindInCachedZones()
        {
            //Unity 的可序列化 array 欄位載入後是空陣列而不是 null，所以只判 Length
            for (var i = 0; i < _cachedZones.Length; i++)
            {
                var zone = _cachedZones[i];
                if (IsMatch(zone))
                    return zone;
            }

            return null;
        }

        private bool IsMatch(EffectZone zone)
        {
            return zone != null && zone.ZoneType == _zoneType && zone.CoversHierarchy;
        }

        public override string Description =>
            $"Under [{(_zoneType != null ? _zoneType.name : "?")}] Zone"
            + (_lookup == ZoneLookupMode.Dynamic ? "" : $" ({_lookup})");
    }
}
