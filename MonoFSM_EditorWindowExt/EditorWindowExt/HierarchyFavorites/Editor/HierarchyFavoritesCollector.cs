using System.Collections.Generic;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    internal static class HierarchyFavoritesCollector
    {
        public static List<HierarchyFavoritesHolder> GetActiveHolders()
        {
            var results = new List<HierarchyFavoritesHolder>();
            foreach (var root in HierarchyFavoritesRootResolver.GetActiveRoots())
                results.AddRange(root.GetComponentsInChildren<HierarchyFavoritesHolder>(true));
            return results;
        }

        public static List<HierarchyFavoriteMarker> GetActiveMarkers()
        {
            var results = new List<HierarchyFavoriteMarker>();
            foreach (var root in HierarchyFavoritesRootResolver.GetActiveRoots())
                results.AddRange(root.GetComponentsInChildren<HierarchyFavoriteMarker>(true));
            return results;
        }

        // 統一資料模型：合併 Holder（舊）與 Marker（新）兩種來源
        public static List<FavoriteGroup> GetActiveGroups()
        {
            var groups = new List<FavoriteGroup>();
            var groupByName = new Dictionary<string, FavoriteGroup>();

            // Holder 每個就是一個 group（沿用舊行為）
            foreach (var holder in GetActiveHolders())
            {
                if (holder == null) continue;
                var group = new FavoriteGroup { Name = holder.GroupName };
                foreach (var entry in holder.Entries)
                {
                    if (entry == null || entry._target == null) continue;
                    group.Items.Add(new FavoriteItem
                    {
                        Target = entry._target,
                        Label = string.IsNullOrEmpty(entry._label) ? entry._target.name : entry._label,
                        Tint = entry._tint,
                    });
                }
                if (group.Items.Count > 0) groups.Add(group);
            }

#if UNITY_EDITOR
            // Marker 依 GroupName 合併（空字串歸 "Markers"）
            foreach (var marker in GetActiveMarkers())
            {
                if (marker == null) continue;
                var name = string.IsNullOrEmpty(marker.GroupName) ? "Markers" : marker.GroupName;
                if (!groupByName.TryGetValue(name, out var group))
                {
                    group = new FavoriteGroup { Name = name };
                    groupByName[name] = group;
                    groups.Add(group);
                }
                group.Items.Add(new FavoriteItem
                {
                    Target = marker.transform,
                    Label = marker.Label,
                    Tint = marker.Tint,
                });
            }
#endif

            return groups;
        }
    }
}
