using MonoFSM.Foundation;
using MonoFSM.Runtime;
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
            //VarEntity 常是 Getter，Value 每讀一次就重算一次來源鏈，只讀一次
            var entity = entityVar != null ? entityVar.Value : null;
            if (entity == null)
            {
                if (Application.isPlaying)
                    Debug.LogError("[TransformOfEntity] Entity variable is null or has no value.", entityVar);
                return null;
            }

            return GetEntityTransform(entity);
        }

        /// <summary>
        ///     已經從 VarEntity 解出 MonoEntity 時用這顆，避免再讀一次 VarEntity.Value。
        ///     有 Animator 就回 Animator 所在的 Transform（視覺 pivot），否則回 entity 本身。
        /// </summary>
        public static Transform GetEntityTransform(MonoEntity entity)
        {
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
