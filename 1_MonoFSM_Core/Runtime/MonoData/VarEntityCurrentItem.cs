using System.Collections.Generic;
using MonoFSM.Core.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Variable
{
    /// <summary>
    ///     「當前項目」鏡射變數：掛在任何 ICurrentEntityOwner 的子物件上即自動接上，
    ///     取值恆等於 owner 的 CurrentEntity，不需要外部化 index 也不需要額外接一顆 provider。
    ///     兩種 owner：
    ///     VarListEntity → list 的 CurrentListItem（外部要引用「list 現在選到誰」時 DropDownRef 指這顆）
    ///     ForEachEntityInListAction → 該輪迭代到的 entity（跑完會回 null，不留殘影）
    ///     用獨立型別（比照 VarIntIndex）而不是裸 VarEntity，是因為 owner 的 children 可能有其他用途的
    ///     VarEntity，共用型別會讓自動接線無從分辨。
    ///     取值來源固定是 parent owner，不吃自己的 valueSource／_defaultValue（沒有 owner 時才 fallback 回 base）。
    /// </summary>
    public class VarEntityCurrentItem : VarEntity
    {
        // 往上抓最近的一顆 owner（list 或 foreach action）。
        // owner 是 list 且進 proxy 模式時 CurrentListItem 自己會 forward，這裡不用處理。
        [ShowInInspector] [AutoParent] private ICurrentEntityOwner _owner;

        // AutoParent 要等 inspector 被點開才解析，但 Description 在畫 hierarchy 時就會被呼叫，
        // editor 下 _owner 幾乎一定還是 null。用 AutoReferenceFieldEditor 當場補解析
        // （[Conditional("UNITY_EDITOR")] + 內部擋 isPlaying，runtime 不會有任何成本），
        // 這樣名字顯示得對，也不用靠 Application.isPlaying 迴避 NRE。
        private ICurrentEntityOwner Owner
        {
            get
            {
                if (_owner == null)
                    AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_owner));
                return _owner;
            }
        }

        // 值由 parent list 決定：隱藏 _defaultValue / _isRuntimeOnly，並讓 Inspector 標成 Getter 而非 Var
        public override bool HasProxySource => true;
        protected override string DescriptionTag => "Getter";

        // hierarchy 上顯示成 [Getter] Current<EntityTag> / <list 名>[i]，跟一般 VarEntity 區分開，
        // 不用點進去也知道這顆是「owner 的當前項目」。
        // 注意：_monoEntityTag 有設時 VarEntity.Rename 會走 Get<Tag> 捷徑，名字不會吃這裡的 Description。
        public override string Description =>
            EntityTag != null
                ? $"Current <{EntityTag.name}>"
                : Owner != null
                    ? Owner.ListDescription + "[i]"
                    : "CurrentItem (no owner)";

#if UNITY_EDITOR
        //owner 跑完一輪後 Value 會回 null（不留殘影），Inspector 上點開這顆只看得到 null。
        //這裡鏡射 owner（foreach）的迭代軌跡，debug 時不用跳回 parent 就知道剛剛實際跑過誰、順序如何。
        //純 getter，沒有序列化也不佔 runtime 成本；owner 沒有迭代語意（VarListEntity）時回 null 不顯示。
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [PropertyTooltip("最近一次 foreach 實際跑過的 entity，依執行順序（reverse 時即反向）")]
        private IReadOnlyList<MonoEntity> DebugIteratedEntities => Owner?.DebugIteratedEntities;
#endif

        protected override MonoEntity GetValueInternal()
        {
            var owner = Owner;
            if (owner == null)
            {
                //editor 下 AutoParent 還沒跑過是常態，不噴 error（會在 inspector 開之前刷滿 console）
                if (Application.isPlaying)
                    Debug.LogError(
                        $"{name} 是 VarEntityCurrentItem，但 parent 找不到 ICurrentEntityOwner"
                        + "（VarListEntity 或 ForEachEntityInListAction），取不到 CurrentItem",
                        this);
                return base.GetValueInternal();
            }

            return owner.CurrentEntity;
        }
    }
}
