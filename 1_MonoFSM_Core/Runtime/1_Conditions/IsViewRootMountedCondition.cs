using _1_MonoFSM_Core.Runtime.MonoData;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 這個 entity 的 ViewRoot 目前是否 mount 在別的 entity 身上（可選：只算 mount 在帶某個 MonoEntityTag 的 entity 上、
    /// 只算 mount 在指定的 mount point 上）。
    /// 掛在道具身上，例如「掛在玩家身上（背包 / 手持 mount）」給偷竊怪篩目標、或把背包裡的東西排除在互動目標外。
    /// 讀的是本地 ViewRoot 的跟隨狀態：SA 端是 MountTo 的結果，其他端是 NetworkedViewRoot / NetworkedBackpackSlot 套用的結果。
    /// </summary>
    public class IsViewRootMountedCondition : AbstractConditionBehaviour
    {
        [Tooltip("要檢查的 entity；留空 = 往上找最近的 MonoEntity")]
        [SerializeField] private VarEntityWrapper _entity;

        [AutoParent] private MonoEntity _selfEntity;

        [Tooltip("選填：只有 mount 目標帶這個 tag（DefaultTag 或 DescriptableTags）才算。留空 = mount 在任何 entity 上都算")]
        [SOConfig("MonoEntityTag")]
        [SerializeField] private MonoEntityTag _attachEntityTag;

        [Tooltip("選填：只有 mount 在這個 mount point（ViewRoot.MountPointTarget）上才算。留空 = 不看 mount point。" +
                 "例：插槽判斷「鴿子是不是掛在我的 AttachPoint 上」，被拔走拿在手上時 mount point 是手，不算")]
        [SerializeField] private Transform _mountPoint;

        public enum Result
        {
            NotEvaluated,
            Mounted,
            NoEntity,
            NoViewRoot,
            NotMounted,
            TagMismatch,
            MountPointMismatch, // 有 mount，但不是 mount 在 _mountPoint 上
        }

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private Result _lastResult;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private MonoEntity _lastAttachEntity;

        protected override bool IsValid
        {
            get
            {
                _lastResult = Evaluate();
                return _lastResult == Result.Mounted;
            }
        }

        private Result Evaluate()
        {
            var entity = _entity != null && _entity.Value != null ? _entity.Value : _selfEntity;
            if (entity == null) return Result.NoEntity;

            var viewRoot = entity.ViewRoot;
            if (viewRoot == null) return Result.NoViewRoot;

            var attach = viewRoot.AttachToEntity;
            _lastAttachEntity = attach;
            if (attach == null) return Result.NotMounted;

            if (_mountPoint != null && viewRoot.MountPointTarget != _mountPoint)
                return Result.MountPointMismatch;

            if (_attachEntityTag == null) return Result.Mounted;
            if (attach.DefaultTag == _attachEntityTag) return Result.Mounted;

            var tags = attach.DescriptableTags;
            if (tags != null && tags.Contains(_attachEntityTag)) return Result.Mounted;
            return Result.TagMismatch;
        }

        public override string Description =>
            _attachEntityTag == null ? "ViewRoot Mounted" : $"ViewRoot Mounted on <{_attachEntityTag.name}>";
    }
}
