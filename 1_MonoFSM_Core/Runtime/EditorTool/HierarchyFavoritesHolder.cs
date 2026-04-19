using System;
using System.Collections.Generic;
using UnityEngine;

namespace HierarchyFavorites
{
    public class HierarchyFavoritesHolder : MonoBehaviour
    {
        [Serializable]
        public class FavoriteEntry
        {
            [SerializeField] public Transform _target;
            [SerializeField] public string _label;
            [SerializeField] public Color _tint = Color.white;
        }

        [SerializeField] private string _groupName = "";
        [SerializeField] private List<FavoriteEntry> _entries = new();

        public string GroupName => string.IsNullOrEmpty(_groupName) ? gameObject.name : _groupName;
        public IReadOnlyList<FavoriteEntry> Entries => _entries;
    }
}
