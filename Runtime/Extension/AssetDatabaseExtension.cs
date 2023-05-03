#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RCGMaker.Core
{
    public static class AssetDatabaseExtension
    {
        public static string GetGUID(this Object obj)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId);
            return guid;
        }
    }
}
#endif