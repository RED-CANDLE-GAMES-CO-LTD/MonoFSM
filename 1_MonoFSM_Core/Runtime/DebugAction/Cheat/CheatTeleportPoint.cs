using System.Collections.Generic;
using System.Linq;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    ///     Cheat 用傳送點標記：放到場景上就會被 CheatTeleportPoints 自動蒐集並分配 Alt+1、Alt+2… 的順序。
    ///     排序規則：先比 _order（小的在前），相同再比 GameObject 名稱。
    /// </summary>
    public class CheatTeleportPoint : AbstractDescriptionBehaviour
    {
        protected override string DescriptionTag => "CheatPoint";
        public override string Description => $"Alt+{OrderIndexForDisplay} 傳送點";

        [Tooltip("手動指定順序，小的排前面；留 0 就照名稱排序")] [SerializeField]
        private int _order;

        public int Order => _order;

        private static readonly List<CheatTeleportPoint> _allPoints = new();

        private void OnEnable()
        {
            if (!_allPoints.Contains(this))
                _allPoints.Add(this);
        }

        private void OnDisable()
        {
            _allPoints.Remove(this);
        }

        /// <summary>
        ///     場上所有啟用中的傳送點，已依 _order、名稱排好序（即 Alt+1、Alt+2… 的順序）。
        /// </summary>
        public static List<CheatTeleportPoint> GetSortedPoints()
        {
            _allPoints.RemoveAll(p => p == null);
            return _allPoints.OrderBy(p => p._order).ThenBy(p => p.name).ToList();
        }

        //Inspector / Gizmo 顯示用，找不到就顯示 ?
        private string OrderIndexForDisplay
        {
            get
            {
                var index = GetSortedPoints().IndexOf(this);
                return index >= 0 ? (index + 1).ToString() : "?";
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f,
                $"Alt+{OrderIndexForDisplay} {name}");
#endif
        }
    }
}
