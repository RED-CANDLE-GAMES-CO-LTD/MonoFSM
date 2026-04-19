using UnityEngine;

namespace HierarchyFavorites
{
    // 直接掛在欲收藏的 GameObject 上，避免 List override 互相覆蓋的問題。
    // Build 時只剩空殼 class，序列化欄位皆被條件編譯移除，
    // 但 type 本身保留 → 不會出現 Missing Script、Photon NetworkObject hash 也維持一致。
    [DisallowMultipleComponent]
    public class HierarchyFavoriteMarker : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private string _groupName = "";
        [SerializeField] private string _label = "";
        [SerializeField] private Color _tint = Color.white;

        public string GroupName => _groupName;
        public string Label => string.IsNullOrEmpty(_label) ? gameObject.name : _label;
        public Color Tint => _tint;
#endif
    }
}
