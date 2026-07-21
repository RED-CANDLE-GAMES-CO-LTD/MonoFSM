using MonoFSM.Foundation;
using MonoFSM.Runtime.Variable;
using UnityEngine;

namespace MonoValueProvider
{
    /// <summary>
    ///     從 VarEntity 取得對應 Transform / 位置的共用工具。
    ///     （原本在 MonoFSM-Pro/Vec3AverageFromEntity.cs，因 TargetPositionResolver 下放到 Core 而一併搬入）
    /// </summary>
    public static class TransformOfEntity
    {
        public static Transform GetEntityTransform(VarEntity entityVar)
        {
            if (entityVar == null || entityVar.Value == null)
            {
                if (Application.isPlaying)
                    Debug.LogError("[TransformOfEntity] Entity variable is null or has no value.", entityVar);
                return null;
            }

            var entity = entityVar.Value;
            //FIXME: 用個pivot?
            var anim = entity.GetCompCache<Animator>();
            if (anim != null)
                return anim.transform;

            return entity.transform;
        }

        public static Vector3 GetEntityPosition(VarEntity entityVar)
        {
            var t = GetEntityTransform(entityVar);
            return t != null ? t.position : Vector3.zero;
        }
    }
}
