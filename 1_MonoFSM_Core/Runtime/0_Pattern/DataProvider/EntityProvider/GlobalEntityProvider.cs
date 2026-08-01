using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     提供 Global Instance 的 Provider，參考 ParentBlackboardProvider 的實作方式
    ///     FIXME: 怎麼拿到目前player的Inventory? condition? runtime tag? (LocalPlayer,Inventory?) tuple key?
    /// </summary>
    /// 改名叫做world?
    public class GlobalEntityProvider : AbstractEntitySource, IEntityValueProvider
    {
        // [Required] [TypeRestrictFilter(typeof(MonoEntity), true, "請選擇 MonoEntity 類型的 VariableTag")] [SerializeField]
        // private MonoEntityTag _monoEntityTag;

        public override string SuggestDeclarationName => "world";

        [PreviewInInspector]
        public override MonoEntity monoEntity => GetBlackboardFromGlobalInstance();

        // public MonoEntityTag entityTag => _monoEntityTag;

        private MonoEntity GetBlackboardFromGlobalInstance()
        {
            if (Application.isPlaying == false)
                return null;
            if (_expectedEntityTag == null)
            {
                if (Application.isPlaying)
                    Debug.LogError("MonoEntityTag is null", this);
                return null;
            }

            var instance = this.GetGlobalInstance(_expectedEntityTag);
            if (instance == null && Application.isPlaying)
            {
                Debug.LogError(
                    $"Global instance not found for tag: {_expectedEntityTag.name}",
                    this
                );
                return null;
            }

            // 如果 instance 是 MonoBlackboard，直接回傳
            // if (instance is MonoEntity blackboard)
            //     return blackboard;

            // 否則嘗試從 instance 取得 MonoBlackboard component
            return instance;
        }

        public string Description => $"Global Instance: {_expectedEntityTag?.name ?? "None"}";
        public MonoEntity Value => monoEntity;
    }
}
