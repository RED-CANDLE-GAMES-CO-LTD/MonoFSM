using UnityEditor;

namespace MonoFSM.InternalBridge
{
    internal static class GlobalObjectIdBridge
    {
        public static GlobalObjectId SetSceneObjectId(this GlobalObjectId old,ulong objectId,ulong prefabId)
        {
            old.m_SceneObjectIdentifier = new SceneObjectIdentifier()
            {
                TargetObject = objectId,
                TargetPrefab = prefabId,
            };
            return old;
        }
    }
}