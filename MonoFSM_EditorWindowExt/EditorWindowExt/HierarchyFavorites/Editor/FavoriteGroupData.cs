using System.Collections.Generic;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    internal class FavoriteItem
    {
        public Transform Target;
        public string Label;
        public Color Tint;
    }

    internal class FavoriteGroup
    {
        public string Name;
        public List<FavoriteItem> Items = new();
    }
}
